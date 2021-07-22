param([string]$branch = "rc4", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = @{
    core = "v3.0.0-rc4";
    devpack = "v3.0.0-rc4";
    modules = "v3.0.0-rc4";
    node = "v3.0.0-rc4";
    vm = "v3.0.0-rc4"
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
