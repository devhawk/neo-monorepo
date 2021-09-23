param([string]$branch = "rel-302", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = "core","devpack","modules","node" ,"vm"

$projects = @{
    vm = "v3.0.0"
    core = "v3.0.2";
    devpack = "v3.0.2";
    modules = "9ab757dde7da9b00c2169e390a66d6e70dc4cf89";
    node = "v3.0.2";
}

git fetch --all
foreach ($kvp in $projects.GetEnumerator()) {
    $prj = $kvp.Key
    $commit = $kvp.Value
    git subtree pull --prefix $prj "official-$prj" $commit --squash
}

if ($merge) {
    write-host "Merging $branch into monorepo-$branch" -ForegroundColor Cyan; 
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
