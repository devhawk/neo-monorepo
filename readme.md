# Neo3 MonoRepo

``` shell
git remote add -f official-core https://github.com/neo-project/neo.git
git remote add -f official-devpack https://github.com/neo-project/neo-devpack-dotnet.git
git remote add -f official-modules https://github.com/neo-project/neo-modules.git
git remote add -f official-vm https://github.com/neo-project/neo-vm.git

git subtree add --prefix vm official-vm v3.0.0-preview2 --squash
git subtree add --prefix core official-core v3.0.0-preview2 --squash
git subtree add --prefix modules official-modules v3.0.0-preview2 --squash
git subtree add --prefix devpack official-devpack v3.0.0-preview2 --squash

git subtree pull --prefix vm official-vm master --squash
git subtree pull --prefix core official-core master --squash
git subtree pull --prefix modules official-modules master --squash
git subtree pull --prefix devpack official-devpack master --squash

```
