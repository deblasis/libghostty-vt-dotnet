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
# evidence that the publish chain is checked. `${{ … }}` is GitHub's rendering
# of the same value, so both spellings are stripped first.
def constant_false?(expression)
  cleaned = expression.to_s.gsub(/\$\{\{|\}\}/, "").strip
  cleaned.match?(/\A(?:false|0|'false'|"false")\z/)
end

# A step-level `if:` decides on the runner whether a gate step runs at all, and
# the probe executes the step body but never evaluates the condition, so the
# condition must be a fact the check can rely on being equally true on every
# run: a property of the build matrix (`startsWith(matrix.rid, 'win-')`).
# Contexts that vary by event (`github.*`), by input, by secret, by another
# job's outcome (`needs.*`, the status functions) or by runner identity can
# skip the gate on exactly the runs that publish, which is the round-4b
# step-`if:` bypass.
def matrix_structural?(condition)
  cleaned = condition.to_s.gsub(/\$\{\{|\}\}/, "").strip
  return false unless cleaned.match?(/\bmatrix\./)
  return false if cleaned.match?(/\b(github|env|inputs|vars|secrets|needs|runner)\b/)
  return false if cleaned.match?(/\b(failure|success|cancelled|always)\s*\(/)

  true
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

# A gate command can be present as its own logical command and still be skipped
# by a shell condition. That is proven behaviourally (the stub must record the
# invocation under both expression substitutions), so there is no static
# rejection of `if`/`for`/`while` here: a condition on files the probe stages is
# legitimate and would otherwise be reported as a bypass.

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
# replaced by a stub that logs its invocation and exits `code`.
#
# Actions substitutes every `${{ ... }}` before the shell sees the body, and
# the check cannot know the runtime value. Each body is therefore run twice,
# once with every expression empty and once with a non-empty literal. A step
# that only reaches its gate for one of those values can be skipped on the
# runner by a condition the probe would otherwise never exercise.
#
# The stub records the command line so the caller can require the gate to have
# been reached, not merely that the step failed for some other reason.
def probe(body, env, code, substitute:)
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

    log = File.join(directory, "invoked.log")
    File.write(log, "")
    STUBBED.each do |name|
      path = File.join(bin, name)
      File.write(path, "#!/bin/sh\nprintf '%s\\n' \"$0 $*\" >> \"$PROBE_LOG\"\nexit #{code}\n")
      File.chmod(0o755, path)
    end

    script = File.join(directory, "step.sh")
    File.write(script, body.to_s.gsub(/\$\{\{[^}]*\}\}/, substitute))

    # A real runner's workspace is a checkout: it has a `.git` and its package
    # artifacts carry real names (`*.nupkg`, not `probe.nupkg`). The probe's
    # work dir must look the same, or a condition on any of those differences
    # is true in one world and false in the other (SEC-R4B-1).
    FileUtils.mkdir_p(File.join(work, ".git"))
    %w[probe.nupkg probe.snupkg].each do |staged|
      FileUtils.rm_f(File.join(work, "artifacts", staged))
    end
    File.write(File.join(work, "artifacts", "package.1.0.0.nupkg"), "")
    File.write(File.join(work, "artifacts", "package.1.0.0.snupkg"), "")

    probe_env = {
      "PATH" => "#{bin}:#{ENV.fetch("PATH", "")}",
      "CI" => "true",
      "GITHUB_ACTIONS" => "true",
      "GITHUB_WORKSPACE" => work,
      "GITHUB_OUTPUT" => File.join(directory, "output"),
      "GITHUB_ENV" => File.join(directory, "env"),
      "RUNNER_TEMP" => File.join(directory, "tmp"),
      "PROBE_LOG" => log,
    }.merge(env)
    ok = system(probe_env, "bash", "-e", script, chdir: work, out: File::NULL, err: File::NULL)
    [ok, File.read(log)]
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
  job_gates = Hash.new { |hash, key| hash[key] = [] }
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
      # `python3 "$NUCHK"` with `env: NUCHK: /dev/null` hides the gate from
      # every literal scan while a same-script invocation elsewhere in the
      # chain keeps `found` satisfied. Substitute known env values into the
      # commands so the alias resolves to what the runner would run.
      step_env = job_env.merge(step["env"] || {})
      expand_env = lambda do |text|
        text.gsub(/\$\{?([A-Z_][A-Z0-9_]*)\}?/) do
          value = step_env[Regexp.last_match(1)]
          value.nil? ? Regexp.last_match(0) : value.to_s
        end
      end
      expanded = commands.map { |command| expand_env.call(command) }
      invoked = GATES.select { |script| expanded.any? { |command| gate_command(command, script) } }
      mentioned = GATES.select { |script| commands.any? { |command| command.include?(script) } }
      consumer = expanded.any? { |command| consumer_command(command) }
      consumer_mentioned = commands.any? { |command| command.include?(CONSUMER_PROJECT) }
      # An interpreter invocation whose script argument still holds an
      # unresolved variable after env substitution is either an alias to
      # something outside the repo (the env-alias no-op) or a path the
      # check cannot resolve — either way it is not a gate the check sees.
      variable_routed = commands.select do |command|
        command.lstrip.match?(/\A(?:python3?|dotnet)\s+"\$[A-Za-z_][A-Za-z0-9_]*"?(\s|\z)/)
      end
      unless variable_routed.empty?
        red << "#{file}: the #{name} job routes a gate/consumer script through a " \
               "variable the check cannot resolve: #{variable_routed.first.strip}"
      end
      # PROBE_LOG exists only in the probe's environment; a condition on it
      # is true under the check and false on every real runner (SEC-R4B-1).
      if body.include?("PROBE_LOG")
        red << "#{file}: the #{(invoked + mentioned).uniq.join(', ')} step reads PROBE_LOG, " \
               "which exists only in the wiring check's probe"
      end

      if consumer
        unless body.include?("GhosttyVtVersionOverride")
          red << "#{file}: the #{CONSUMER_LABEL} step does not set GhosttyVtVersionOverride"
        end
      end
      next if mentioned.empty? && !consumer && !consumer_mentioned

      invoked.each do |script|
        found[script] = true
        job_gates[name] << script
      end

      if step.key?("if")
        if consumer
          red << "#{file}: the #{CONSUMER_LABEL} step is behind an if: (#{step["if"]}), so it can be skipped"
        elsif constant_false?(step["if"])
          red << "#{file}: the #{name} step is behind a constant-false if: (#{step["if"]})"
        elsif !matrix_structural?(step["if"])
          # The probe runs the body but never evaluates this condition, so a
          # condition on anything but the matrix can skip the gate on the
          # runner while the check stays green.
          red << "#{file}: the #{(invoked + mentioned).uniq.join(', ')} step is behind an " \
                 "if: (#{step["if"].to_s.strip}) the check cannot treat as " \
                 "always-true; only matrix conditions are allowed on gate steps"
        end
      end
      label = consumer ? CONSUMER_LABEL : (invoked + mentioned).uniq.join(", ")
      red << "#{file}: the #{label} step is continue-on-error" if step["continue-on-error"]
      if step.key?("shell")
        red << "#{file}: the #{label} step overrides the shell (#{step["shell"]})"
      end
      if disables_errexit?(body)
        red << "#{file}: the #{label} step disables errexit (set +e / set +o errexit / trap EXIT)"
      end

      # `dotnet test --list-tests` exits 0 without running a test, and
      # `--filter` with zero matches prints "No test matches" and also exits
      # 0 — VSTest treats an empty selection as success. Either way the step
      # is green on a package that does not load.
      if consumer && body.match?(/--list-tests\b|--filter\b/)
        red << "#{file}: the #{CONSUMER_LABEL} step does not run the tests " \
               "(--list-tests / --filter): #{body[/--(list-tests|filter)[^\n]*/]&.strip}"
      end

      # The invocation must be the whole command. `if [ ... ]; then python3
      # gate.py; fi` is one logical command that contains the script and is
      # therefore invisible to a rule that only looks for commands starting
      # with the interpreter.
      mentioned.each do |script|
        commands.each do |command|
          next unless command.include?(script)
          next if gate_command(command, script)

          red << "#{file}: the #{script} invocation is wrapped instead of run as its own command: " \
                 "#{command.strip}"
        end
      end
      if consumer_mentioned
        commands.each do |command|
          next unless command.include?(CONSUMER_PROJECT)
          next if consumer_command(command)

          red << "#{file}: the #{CONSUMER_LABEL} invocation is wrapped instead of run as its own " \
                 "command: #{command.strip}"
        end
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
      # fails when the command it runs fails. Both expression substitutions are
      # required, and the stub must record that the gate was reached: a step
      # that exits 0 early, or one whose condition is false only for the value
      # the runner supplies, is not a gate even when its body contains one.
      step_env = job_env.merge(step["env"] || {})
      expected = invoked + (consumer ? [CONSUMER_PROJECT] : [])
      { "" => "expressions empty", "probe-expression" => "expressions non-empty" }.each do |substitute, flavour|
        ok, calls = probe(body, step_env, 1, substitute: substitute)
        if ok
          red << "#{file}: the #{label} step does not fail when its gate fails (#{flavour})"
        elsif expected.any? && expected.none? { |needle| calls.include?(needle) }
          red << "#{file}: the #{label} step fails before reaching its gate (#{flavour})"
        end
        # The failure probe stops at the first failing gate, so only the success
        # probe can require every gate in the step to have been reached.
        ok, calls = probe(body, step_env, 0, substitute: substitute)
        unless ok
          red << "#{file}: the #{label} step fails when its gate succeeds (#{flavour})"
        end
        missing = expected.reject { |needle| calls.include?(needle) }
        unless missing.empty?
          red << "#{file}: the #{label} step does not reach #{missing.join(', ')} " \
                 "(#{flavour}), so a condition can skip it on the runner"
        end
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
  # Each job that runs the ABI check must also run the self-test that proves the
  # check can fail. Keeping the pair in one job means deleting either one of
  # several per-file invocations is visible, and a job that skips the check
  # cannot rely on the other job's self-test.
  job_gates.each do |name, scripts|
    next unless scripts.include?("build/check-windows-abi.py")
    next if scripts.include?("build/selftest-windows-abi.py")

    red << "#{file}: job #{name} runs build/check-windows-abi.py without " \
           "build/selftest-windows-abi.py, so the check is unverified there"
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
      # `failure()` alone (or as a conjunct) makes publish run exactly when a
      # need failed; `success()` is the default; `cancelled()`/`always()`
      # override the implicit `success()`. Any status function means the
      # publish decision is explicit, so the required result term must be a
      # top-level conjunct of a plain conjunction in that case.
      has_status_function = condition.match?(
        /\b(?:failure|success|cancelled|always)\s*\(\s*\)/
      )
      next unless has_status_function

      # The result term must be a top-level conjunct of a plain conjunction.
      # `A || term`, `term || true`, or `(term || B)` all let the publish run
      # while the consumer job failed. Parentheses around the status functions
      # are part of the allowed shape, so strip those before looking for
      # grouping.
      residual = condition.gsub(/!?\s*(?:cancelled|always|failure|success)\s*\(\s*\)/, "")
      grouped = residual.include?("||") || residual.include?("(") || residual.include?(")")
      required = "needs.#{consumer}.result == 'success'"
      conjuncts = condition.split("&&").map { |term| term.strip.gsub(/\s+/, " ") }
      unless !grouped && conjuncts.include?(required)
        red << "#{file}: publish can run after #{consumer} fails; add " \
               "#{required} to its if:"
      end
    end
  end
end

unless red.empty?
  red.each { |line| puts line }
  exit 1
end

puts "package gates wired: #{files.join(', ')}"
