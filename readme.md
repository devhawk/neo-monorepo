# Neo3 MonoRepo

``` shell
git remote add -f official-core https://github.com/neo-project/neo.git
git remote add -f official-devpack https://github.com/neo-project/neo-devpack-dotnet.git
git remote add -f official-modules https://github.com/neo-project/neo-modules.git
git remote add -f official-vm https://github.com/neo-project/neo-vm.git
git remote add -f official-node https://github.com/neo-project/neo-node.git

git subtree add --prefix vm official-vm v3.0.0-preview2 --squash
git subtree add --prefix core official-core v3.0.0-preview2 --squash
git subtree add --prefix modules official-modules v3.0.0-preview2 --squash
git subtree add --prefix devpack official-devpack v3.0.0-preview2 --squash
git subtree add --prefix node official-node v3.0.0-preview3 --squash

```
