# PreToolUse (Bash y PowerShell): rechaza los git que ya nos costaron (D-247).
# Exit 2 = rechazar; el mensaje de stderr le llega a Claude.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$cmd = [string]$payload.tool_input.command
if (-not $cmd) { exit 0 }

# Sin el texto entre comillas: un mensaje de commit puede decir "git add -A".
$bare = [regex]::Replace($cmd, '"[^"]*"|''[^'']*''', '""')

function Block($why) { [Console]::Error.WriteLine("git-guard: $why"); exit 2 }

if ($bare -match '\bgit\s+(-\S+\s+)*add\b[^;&|]*?(\s-A\b|\s--all\b|\s\.(\s|$|;|&|\|))') {
    Block 'no git add -A / git add . (arrastra carpetas sueltas); agrega las rutas una por una.'
}
if ($bare -match '\bgit\s+(-\S+\s+)*commit\b') {
    if ($bare -match '\bgit\s+(-\S+\s+)*commit\b[^;&|]*?(\s-[A-Za-z]*a[A-Za-z]*(\s|$)|\s--all\b)') {
        Block 'no git commit -a; usa git commit -m "..." -- <rutas>.'
    }
    $dir = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $branch = (& git -C $dir branch --show-current 2>$null)
    if ($branch -eq 'main') { Block 'rama actual main (D-5, la sube el usuario); commitea en TrainingApp o spec-*.' }
}
exit 0
