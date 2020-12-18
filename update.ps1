param([switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

# if ($currentBranch -ne $branch) {
#     throw "wrong branch $branch"
# }

# vm      tagged preview 4 @ 27dd6758291cf6cf64390598707f98faac3845c0
# core    tagged preview 4 @ 414dab140025f46898a3afee3a59a4e18b72d2b2
# devpack tagged preview 4 @ b8e0b96a5206d657e69ccd868da8d7a490ef4775
# modules tagged preview 4 @ 78e30fd7d12fdecfca73f491a35ead6a7460d832
# node    tagged preview 4 @ 5809bd5998c9ca56b723629742d057433b51a65d

$projects = @{ 
    core = "414dab140025f46898a3afee3a59a4e18b72d2b2"; 
    vm = "27dd6758291cf6cf64390598707f98faac3845c0"; 
    devpack = "b8e0b96a5206d657e69ccd868da8d7a490ef4775";
    modules = "78e30fd7d12fdecfca73f491a35ead6a7460d832";
    node = "5809bd5998c9ca56b723629742d057433b51a65d" 
    }

git fetch --all
foreach ($prj in $projects.keys) {
    git subtree pull --prefix $prj "official-$prj" "$($projects[$prj])" --squash
}

if ($merge) {
    git checkout "monorepo-$currentBranch"
    git merge $currentBranch
}
