# Neo3 MonoRepo

This repo pulls together the code from [neo](https://github.com/neo-project/neo),
[neo-vm](https://github.com/neo-project/neo-vm), [neo-modules](https://github.com/neo-project/neo-modules)
and [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet)
into a single repo. Additionally, project dependencies in branches using the `monorepo-` prefix
have been updated to use intra-repo project references instead of pulling CI packages from MyGet.
This way, you can compile the major parts the Neo platform together in one place.

This repo uses `git subtree` to combine the contents of those four repos into one. For more
background on this command, please see [this great article](https://www.atlassian.com/git/tutorials/git-subtree)
from Atlassian.

## Branch Configuration

This repo has multiple branches, tracking different Neo releases. The `master` branch of this
repo tracks the master branch of the four repos I'm pulling in. There is also a local `preview-2`
branch that tracks the preview2 tagged commits in the other repos. I will be creating
corresponding local branches to track future Neo 3 preview releases.

For each local repo branch, there is a branch prefixed `monorepo-` that has the modified project
dependencies. So while `master` branch CI packages from MyGet, `monorepo-master` branch uses intra-repo package references. Same for `preview-2` and `monorepo-preview2`.

## Setup

> Note, I'm including this section for those who are interested in understanding how I created
> this repo. If you're just using the repo, you just need to clone it locally and check out
> the branch you want to use.

My local clone of this repo has five remotes - one for the neo-monorepo remote hosted on
GitHub and one for each of the other repos I'm pulling code from. For the four remotes I'm
pulling from, I'm using the prefix `official-`. I created these remotes using the `git remote`
command like this:

``` shell
git remote add -f official-core https://github.com/neo-project/neo.git
git remote add -f official-devpack https://github.com/neo-project/neo-devpack-dotnet.git
git remote add -f official-modules https://github.com/neo-project/neo-modules.git
git remote add -f official-node https://github.com/neo-project/neo-node.git
git remote add -f official-vm https://github.com/neo-project/neo-vm.git
```

Technically, using remotes is optional, but it seemed like the right approach since I will be
keeping this repo up to date with changes from the other repos.

Once I configured the remotes, I pulled the `v3.0.0-preview2` tagged code into the repo. I used
that tag as it was the earliest code I cared about tracking. Note, I'm squashing the commits here
to keep the history easier to navigate. I *don't* use this monorepo for making changes, though
`git subtree` does support push as well as pull. Personal preference I guess.

``` shell
git subtree add --prefix vm official-vm v3.0.0-preview2 --squash
git subtree add --prefix core official-core v3.0.0-preview2 --squash
git subtree add --prefix modules official-modules v3.0.0-preview2 --squash
git subtree add --prefix devpack official-devpack v3.0.0-preview2 --squash
```

The `git subtree add` command maps an entire remote repo to a specific folder in the local repo.
Once I had the subtrees created, I marked that commit as the `preview-2` branch and updated
the local `master` branch to remote master commits using `git subtree pull`. I have a local
PowerShell script update.ps1 that I use to run this update regularly

``` shell
git subtree pull --prefix vm official-vm master --squash
git subtree pull --prefix core official-core master --squash
git subtree pull --prefix modules official-modules master --squash
git subtree pull --prefix devpack official-devpack master --squash
git subtree pull --prefix node official-node master --squash
```

Once I update the `master` branch, I merge those changes into the `monorepo-master` branch,
manually fixing up any changes to project files.
