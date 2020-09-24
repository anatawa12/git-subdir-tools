
## the message of commit format

the message of the copied commit in subdir repository is like shown below:

```
<base-commit-message>
Git-Subdir-Tools-Basedir-Commit: <commit-ref>
```

the message of the copied commit to rootdir repository from subdir repository is like shown below:

```
<base-commit-message>
Git-Subdir-Tools-Subdir-Commit: <path-of-subdir>: <commit-ref>
```

The format of `<commit-ref>` is like shown below:

```
<repository-ref>@<sha1-commit-hash>
```

The `<repository-ref>` is a any string which describes the repository is exists at.
The `<repository-ref>` is exists for human-readable so there is no required format.
It's recommended to use the URL of git repository like `ssh://git@github.com/anatawa12/git-subdir-tools` 
or identifier of repository in your hosting service like `anatawa12/git-subdir-tools`
if the subdir repository and rootdir repository are same hosting service.
