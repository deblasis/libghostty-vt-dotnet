#!/usr/bin/env ruby
# frozen_string_literal: true
#
# Check that every workflow the publish chain depends on runs the package gates
# and their self-tests, and that nothing can turn a failing gate green.
#
# The gates only protect a push if the job that publishes runs them. A deleted
# step, a step moved behind `if:`, a gate piped into `cat`, or a self-test that
# stops being invoked all leave the workflow parseable and the YAML looking
# right, so this executes nothing and asserts on the structure instead.
#
# Usage: build/check-package-gate-wiring.rb [workflow.yml ...]
# Defaults to the two workflows that pack and push Windows natives.

require "yaml"

GATES = [
  "build/check-nupkg.py",
  "build/check-snupkg.py",
  "build/selftest-check-nupkg.py",
  "build/selftest-check-snupkg.py",
].freeze

def commands(body)
  body.to_s.lines.reject { |line| line.lstrip.start_with?("#") }.join
end

# A line whose whole command is the gate. A substring match would accept
# `echo "python3 build/check-nupkg.py ..."`, which runs nothing.
def gate_lines(step, script)
  commands(step["run"]).lines.select do |line|
    line =~ /^\s*(?:python3?|python)\s+#{Regexp.escape(script)}(?=\s|$)/
  end
end

# The exit status of a gate stops reaching the step when it is chained, piped,
# backgrounded or wrapped. `&&` is included because a failing gate followed by
# a successful command is the same bypass as `|| true`.
def chained?(line)
  line.include?("||") || line.include?(";") || line.include?("|") ||
    line.include?("&&") || line.lstrip.start_with?("(") ||
    line.rstrip.end_with?("&")
end

files = ARGV.empty? ? [".github/workflows/daily-upstream.yml", ".github/workflows/release.yml"] : ARGV
red = []

files.each do |file|
  jobs = (YAML.load_file(file)["jobs"] || {})
  roots = jobs.keys.select { |name| name =~ /publish|pack/ }
  seen = {}
  stack = roots.dup
  until stack.empty?
    name = stack.pop
    next if seen[name]

    seen[name] = true
    Array(jobs[name]["needs"]).each { |dep| stack << dep }
  end

  found = Hash.new(false)
  tree = false
  download = false

  seen.each_key do |name|
    (jobs[name]["steps"] || []).each do |step|
      GATES.each do |script|
        lines = gate_lines(step, script)
        next if lines.empty?

        found[script] = true
        red << "#{file}: the #{script} step is behind an if: (#{step["if"]})" if step.key?("if")
        red << "#{file}: the #{script} step is continue-on-error" if step["continue-on-error"]
        if step.key?("shell")
          red << "#{file}: the #{script} step overrides the shell (#{step["shell"]})"
        end
        lines.each do |line|
          if chained?(line)
            red << "#{file}: the #{script} step is chained or wrapped: #{line.strip}"
          end
        end
      end

      body = commands(step["run"])
      tree ||= body.include?("runtimes/win-arm64/native") if body.include?("build/check-nupkg.py")
      if step["uses"].to_s.start_with?("actions/download-artifact") &&
         (step["with"] || {})["name"] == "native-win-arm64"
        download ||= step["continue-on-error"] == true
      end
    end
  end

  GATES.each do |script|
    red << "#{file}: publish chain never runs #{script}" unless found[script]
  end
  red << "#{file}: the nupkg gate does not look for the win-arm64 tree" unless tree
  red << "#{file}: the win-arm64 artifact download is not allowed to fail" unless download
end

unless red.empty?
  red.each { |line| puts line }
  exit 1
end

puts "package gates wired: #{files.join(', ')}"
