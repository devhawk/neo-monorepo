param([string]$branch = "develop", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = @{core="develop";devpack="master";modules="develop";node="master";vm="master"}

git fetch --all
foreach ($kvp in $projects.GetEnumerator()) {
    $prj = $kvp.Key
    $prjBranch = $kvp.Value
    write-host $prj -ForegroundColor Cyan;

    git subtree pull --prefix $prj "official-$prj" $prjBranch --squash
}

if ($merge) {
    write-host "Merging $branch into monorepo-$branch" -ForegroundColor Cyan; 
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
