param([string]$branch = "master")

git subtree pull --prefix vm official-vm $branch --squash
git subtree pull --prefix core official-core $branch --squash
git subtree pull --prefix modules official-modules $branch --squash
git subtree pull --prefix devpack official-devpack $branch --squash
