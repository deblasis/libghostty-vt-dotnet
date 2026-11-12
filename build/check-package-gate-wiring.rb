#!/usr/bin/env ruby
# frozen_string_literal: true
#
# Check that every workflow the publish chain depends on runs the package gates,
# their self-tests, and the Windows package consumer test, and that nothing can
# turn a failing gate green.
#
# The gates only protect a push if the job that publishes runs them. A deleted
# step, a step moved behind `if:`, a gate piped into `cat`, a `set +e` earlier in
# the step, a skipped decoy job, or a consumer test the publish job does not
# wait for all leave the workflow parseable and the YAML looking right. Static
# matching alone cannot tell whether an exit status reaches the step, so each
# gate step is also executed with a stub interpreter: once where the stub exits
# 1, which must fail the step, and once where it exits 0, which must not.
#
# Usage: build/check-package-gate-wiring.rb [workflow.yml ...]
# Defaults to the two workflows that pack and push Windows natives.

require "fileutils"
require "tmpdir"
require "yaml"

GATES = [
  "build/check-nupkg.py",
  "build/check-snupkg.py",
  "build/selftest-check-nupkg.py",
  "build/selftest-check-snupkg.py",
  "build/check-windows-abi.py",
  "build/selftest-windows-abi.py",
].freeze

CONSUMER_PROJECT = "tests/Ghostty.Vt.PackageConsumer.Tests/Ghostty.Vt.PackageConsumer.Tests.csproj"
CONSUMER_LABEL = "Windows package consumer test"
STUBBED = %w[python python3 dotnet].freeze

# A job or step guarded by one of these never runs, so a gate inside it is not
# evidence that the publish chain is checked.
def constant_false?(expression)
  expression.to_s.strip.match?(/\A(?:false|0|'false'|"false")\z/)
end

# One shell command, with backslash continuations folded in. Checking each
# physical line would miss `gate \` followed by `|| true`, which bash runs as a
# single command whose status is the `true`.
def logical_commands(body)
  commands = []
  buffer = +""
  body.to_s.each_line do |line|
    stripped = line.rstrip
    next if stripped.lstrip.start_with?("#")

    if stripped.end_with?("\\")
      buffer << stripped[0...-1] << " "
    else
      buffer << stripped
      commands << buffer unless buffer.strip.empty?
      buffer = +""
    end
  end
  commands << buffer unless buffer.strip.empty?
  commands
end

def gate_command(command, script)
  command.lstrip.match?(/\A(?:python3?|python)\s+#{Regexp.escape(script)}(?=\s|\z)/)
end

def consumer_command(command)
  command.match?(/\bdotnet\s+test\s+.*#{Regexp.escape(CONSUMER_PROJECT)}/)
end

# The exit status of a command stops reaching the step when it is chained,
# piped, backgrounded or wrapped. `&&` is included because a failing gate
# followed by a successful command is the same bypass as `|| true`.
def chained?(command)
  command.include?("||") || command.include?(";") || command.include?("|") ||
    command.include?("&&") || command.lstrip.start_with?("(") ||
    command.rstrip.end_with?("&")
end

def disables_errexit?(body)
  body.to_s.lines.any? do |line|
    line.match?(/^\s*set\s+\+e\b/) ||
      line.match?(/^\s*set\s+\+o\s+errexit\b/) ||
      line.match?(/^\s*trap\b.*\bEXIT\b/)
  end
end

# A command that succeeds unconditionally, placed after a gate whose status the
# author wants to discard. Under `bash -e` it is only reachable when errexit was
# disabled, so this is a static companion to the behavioural probe rather than a
# replacement for it.
def neutraliser?(command)
  command.strip.match?(%r{\A(?:true|:|exit(?:\s+0)?|/bin/true)\z})
end

def windows_job?(job)
  runner = job["runs-on"].to_s
  return true if runner.match?(/windows/i)

  matrix = job.dig("strategy", "matrix") || {}
  matrix.values.any? { |value| Array(value).any? { |entry| entry.to_s.match?(/windows/i) } }
end

# Execute a step body the way Actions does (`bash -e`) with python/dotnet
# replaced by a stub that exits `code`. A step whose status does not follow the
# stub is not a gate, whatever its text says.
def probe(body, env, code)
  Dir.mktmpdir("gate-probe-") do |directory|
    bin = File.join(directory, "bin")
    work = File.join(directory, "work")
    FileUtils.mkdir_p(bin)
    FileUtils.mkdir_p(File.join(work, "artifacts"))
    # The gates fail closed when they cannot find a package, so the probe
    # supplies the files a real pack job would have produced.
    File.write(File.join(work, "artifacts", "probe.nupkg"), "")
    File.write(File.join(work, "artifacts", "probe.snupkg"), "")
    FileUtils.mkdir_p(File.join(work, "runtimes", "win-x64", "native"))
    FileUtils.mkdir_p(File.join(work, "runtimes", "win-arm64", "native"))
    FileUtils.mkdir_p(File.join(directory, "tmp"))

    STUBBED.each do |name|
      path = File.join(bin, name)
      File.write(path, "#!/bin/sh\nexit #{code}\n")
      File.chmod(0o755, path)
    end

    script = File.join(directory, "step.sh")
    # Actions substitutes `${{ ... }}` before the shell runs; without that a
    # condition on an expression would be a bash syntax error rather than the
    # branch the runner takes. A literal that matches neither side is chosen
    # deliberately: no expression may make a gate run in the probe unless it
    # also runs on the runner.
    File.write(script, body.to_s.gsub(/\$\{\{[^}]*\}\}/, "probe-expression"))

    probe_env = {
      "PATH" => "#{bin}:#{ENV.fetch("PATH", "")}",
      "CI" => "true",
      "GITHUB_ACTIONS" => "true",
      "GITHUB_WORKSPACE" => work,
      "GITHUB_OUTPUT" => File.join(directory, "output"),
      "GITHUB_ENV" => File.join(directory, "env"),
      "RUNNER_TEMP" => File.join(directory, "tmp"),
      "PKG_VERSION" => "0.0.0",
    }.merge(env)
    system(probe_env, "bash", "-e", script, chdir: work, out: File::NULL, err: File::NULL)
  end
end

files = ARGV.empty? ? [".github/workflows/daily-upstream.yml", ".github/workflows/release.yml"] : ARGV
red = []

files.each do |file|
  document = YAML.load_file(file)
  jobs = document["jobs"] || {}
  publishes = jobs.keys.select { |name| name.match?(/\Apublish\b/) }
  if publishes.empty?
    red << "#{file}: no publish job, so there is no chain to check"
    next
  end

  # Everything the publish job waits for. A gate outside this closure cannot
  # stop a publish and does not count.
  chain = []
  stack = publishes.dup
  until stack.empty?
    name = stack.pop
    next if chain.include?(name)

    chain << name
    Array(jobs.dig(name, "needs")).each { |dep| stack << dep }
  end

  workflow_env = (document["env"] || {})
  skipped = []
  chain.each do |name|
    job = jobs[name] || {}
    if job.key?("if") && constant_false?(job["if"])
      skipped << name
      red << "#{file}: job #{name} is skipped by a constant-false if:, so its gates never run"
    end
    if job["continue-on-error"]
      red << "#{file}: job #{name} is continue-on-error, so a failed gate does not fail the workflow"
    end
  end

  found = Hash.new(false)
  tree = false
  download = false

  # Find every job that runs the consumer test, whether or not the publish job
  # waits for it. A job outside the chain is exactly what a removed `needs`
  # entry looks like, and it must be named rather than silently ignored.
  all_consumer_jobs = jobs.keys.select do |name|
    (jobs.dig(name, "steps") || []).any? do |step|
      logical_commands(step["run"]).any? { |command| consumer_command(command) }
    end
  end
  windows_consumers = all_consumer_jobs.select { |name| windows_job?(jobs[name] || {}) }

  chain.each do |name|
    next if skipped.include?(name)

    job = jobs[name] || {}
    job_env = workflow_env.merge(job["env"] || {})

    (job["steps"] || []).each do |step|
      body = step["run"].to_s
      commands = logical_commands(body)
      invoked = GATES.select { |script| commands.any? { |command| gate_command(command, script) } }
      consumer = commands.any? { |command| consumer_command(command) }

      if consumer
        unless body.include?("GhosttyVtVersionOverride")
          red << "#{file}: the #{CONSUMER_LABEL} step does not set GhosttyVtVersionOverride"
        end
      end
      next if invoked.empty? && !consumer

      invoked.each { |script| found[script] = true }

      if step.key?("if")
        if consumer
          red << "#{file}: the #{CONSUMER_LABEL} step is behind an if: (#{step["if"]}), so it can be skipped"
        elsif constant_false?(step["if"])
          red << "#{file}: the #{name} step is behind a constant-false if: (#{step["if"]})"
        end
      end
      label = consumer ? CONSUMER_LABEL : invoked.join(", ")
      red << "#{file}: the #{label} step is continue-on-error" if step["continue-on-error"]
      if step.key?("shell")
        red << "#{file}: the #{label} step overrides the shell (#{step["shell"]})"
      end
      if disables_errexit?(body)
        red << "#{file}: the #{label} step disables errexit (set +e / set +o errexit / trap EXIT)"
      end
      commands.each_with_index do |command, index|
        next unless invoked.any? { |script| gate_command(command, script) } ||
                    (consumer && consumer_command(command))

        red << "#{file}: the #{label} step is chained or wrapped: #{command.strip}" if chained?(command)
        following = commands[index + 1]
        if following && neutraliser?(following)
          red << "#{file}: the #{label} step continues after the gate with a neutraliser: " \
                 "#{following.strip}"
        end
      end

      # Static checks are a fast fail; this is the actual proof that the step
      # fails when the command it runs fails.
      step_env = job_env.merge(step["env"] || {})
      red << "#{file}: the #{label} step does not fail when its gate fails" if probe(body, step_env, 1)
      unless probe(body, step_env, 0)
        red << "#{file}: the #{label} step fails when its gate succeeds"
      end

      if invoked.include?("build/check-nupkg.py") && body.include?("runtimes/win-arm64/native")
        tree = true
      end
    end

    (job["steps"] || []).each do |step|
      next unless step["uses"].to_s.start_with?("actions/download-artifact")
      next unless (step["with"] || {})["name"] == "native-win-arm64"

      download ||= step["continue-on-error"] == true
    end
  end

  GATES.each do |script|
    red << "#{file}: publish chain never runs #{script}" unless found[script]
  end
  red << "#{file}: the nupkg gate does not look for the win-arm64 tree" unless tree
  red << "#{file}: the win-arm64 artifact download is not allowed to fail" unless download

  if windows_consumers.empty?
    red << "#{file}: no Windows x64 package consumer test (#{CONSUMER_PROJECT})"
  end

  publishes.each do |publish|
    needs = Array(jobs.dig(publish, "needs"))
    windows_consumers.each do |consumer|
      unless chain.include?(consumer) && needs.include?(consumer)
        red << "#{file}: publish does not list the Windows consumer job #{consumer} in needs"
        next
      end

      condition = jobs.dig(publish, "if").to_s
      next unless condition.match?(/!\s*cancelled\(\)|always\(\)/)

      unless condition.match?(/needs\.#{Regexp.escape(consumer)}\.result\s*==\s*'success'/)
        red << "#{file}: publish can run after #{consumer} fails; add " \
               "needs.#{consumer}.result == 'success' to its if:"
      end
    end
  end
end

unless red.empty?
  red.each { |line| puts line }
  exit 1
end

puts "package gates wired: #{files.join(', ')}"
