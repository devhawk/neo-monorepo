param([string]$branch = "preview3")

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    Write-error "wrong branch $branch"
} else {
    git subtree pull --prefix vm official-vm $branch --squash
    git subtree pull --prefix core official-core $branch --squash
    git subtree pull --prefix modules official-modules $branch --squash
    git subtree pull --prefix devpack official-devpack $branch --squash
}
