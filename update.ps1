param([string]$branch = "master", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

# vm project switched to .net 5 after a14b934f5b5c8779164e3b96b276984b3a57ccd4
# exclude from subtree pull for now
$projects = "core","devpack","modules","node" #,"vm"

git fetch --all
foreach ($prj in $projects) {
    git subtree pull --prefix $prj "official-$prj" $branch --squash
}

if ($merge) {
    git checkout "monorepo-$branch"
    git merge $branch
}
