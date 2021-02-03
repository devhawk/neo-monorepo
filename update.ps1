param([string]$branch = "preview5", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = @{
    core = "604541c91b3151ee40a8613cb8534ebc0540fd27";
    devpack = "4902638e16f2cfa9df0e75776b3f83b4197a3601";
    modules = "cc8c218163dd3c86c23dfa7e8b6adc411c4d545c";
    node = "6389ab70592c5188a1bb88a5ce8aa8084a144cc5";
    vm = "f1aff0d89cda2580371521b3323f310155150655"
}

git fetch --all
foreach ($kvp in $projects.GetEnumerator()) {
    $prj = $kvp.Key
    $commit = $kvp.Value
    git subtree pull --prefix $prj "official-$prj" $commit --squash
}

if ($merge) {
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
