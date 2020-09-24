using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitSubdirTools.Libs
{
    public static class TreeUtil
    {
        public static Tree CopyTreeToAnotherRepository(Tree tree, Repository repo)
        {
            return CopyTreeToAnotherRepositoryWithModifying(tree, repo, null);
        }

        public static Tree CopyTreeToAnotherRepositoryWithModifying(Tree tree, Repository repo, TreeModify? modify)
        {
            if ((modify == null || modify.ModifyDescriptor.Count == 0) && repo.ObjectDatabase.Contains(tree.Id))
                return repo.Lookup<Tree>(tree.Id);
            ISet<string> proceedModifyKeys = new HashSet<string>();

            TreeDefinition definition = new TreeDefinition();
            foreach (var entry in tree)
            {
                TreeModify? modifyForMe = null;
                if (modify != null && modify.ModifyDescriptor.TryGetValue(entry.Name, out var modifyDefine))
                {
                    proceedModifyKeys.Add(entry.Name);
                    switch (modifyDefine)
                    {
                        // null means remove
                        case null:
                            continue;
                        case (Blob modBlob, Mode modMode):
                            CopyBlob(definition, entry.Name, modBlob, modMode, repo);
                            continue;
                        case Tree modTree:
                            CopyTree(definition, entry.Name, modTree, repo, null);
                            continue;
                        case GitLink modLink:
                            CopyLink(definition, entry.Name, modLink);
                            continue;
                        case TreeModify modModify:
                            modifyForMe = modModify;
                            break;
                        default:
                            throw new Exception("invalid TreeModify Instance");
                    }
                }

                switch (entry.TargetType)
                {
                    case TreeEntryTargetType.Blob:
                        CopyBlob(definition, entry.Name, (Blob) entry.Target, entry.Mode, repo);
                        break;
                    case TreeEntryTargetType.Tree:
                        CopyTree(definition, entry.Name, (Tree) entry.Target, repo, modifyForMe);
                        break;
                    case TreeEntryTargetType.GitLink:
                        CopyLink(definition, entry.Name, (GitLink) entry.Target);
                        break;
                    default:
                        throw new InvalidOperationException("Target Type out of bounds.");
                }
            }

            if (modify != null)
            {
                foreach (var (key, modifyDefine) in modify.ModifyDescriptor)
                {
                    if (!proceedModifyKeys.Contains(key))
                    {
                        switch (modifyDefine)
                        {
                            // null means remove
                            case null:
                                continue;
                            case (Blob modBlob, Mode modMode):
                                CopyBlob(definition, key, modBlob, modMode, repo);
                                break;
                            case Tree modTree:
                                CopyTree(definition, key, modTree, repo, null);
                                break;
                            case GitLink modLink:
                                CopyLink(definition, key, modLink);
                                break;
                            case TreeModify modModify:
                                CopyTree(definition, key,
                                    repo.Lookup<Tree>("4b825dc642cb6eb9a060e54bf8d69288fbee4904"),
                                    repo, modModify);
                                break;
                            default:
                                throw new Exception("invalid TreeModify Instance");
                        }
                    }
                }
            }

            return repo.ObjectDatabase.CreateTree(definition);
        }

        private static void CopyBlob(TreeDefinition definition, string name, Blob blob, Mode mode, Repository repo)
        {
            Blob newObject;
            if (repo.ObjectDatabase.Contains(blob.Id))
            {
                newObject = repo.Lookup<Blob>(blob.Id);
            }
            else
            {
                using var stream = blob.GetContentStream();
                newObject = repo.ObjectDatabase.CreateBlob(stream);
            }

            definition.Add(name, newObject, mode);
        }

        private static void CopyLink(TreeDefinition definition, string name, GitLink link)
        {
            definition.AddGitLink(name, link.Id);
        }

        private static void CopyTree(TreeDefinition definition, string     name,
            Tree                                    tree,       Repository repo, TreeModify? modifyForMe)
        {
            Tree newObject = CopyTreeToAnotherRepositoryWithModifying(tree, repo, modifyForMe);
            definition.Add(name, newObject);
        }
    }

    public class TreeModify
    {
        internal readonly IDictionary<string, object?> ModifyDescriptor = new Dictionary<string, object?>();

        public void AddOrSet(string key, Blob data, Mode mode)
        {
            AddOrSetImpl(key, (data, mode));
        }

        public void AddOrSet(string key, GitLink gitLinkTo)
        {
            AddOrSetImpl(key, gitLinkTo);
        }

        public void AddOrSet(string key, Tree tree)
        {
            AddOrSetImpl(key, tree);
        }

        public void AddOrSet(string key, TreeModify tree)
        {
            AddOrSetImpl(key, tree);
        }

        public void Remove(string key)
        {
            AddOrSetImpl(key, null);
        }

        private void AddOrSetImpl(string key, object? data)
        {
            try
            {
                AddOrSetImpl(key.Split('/'), data);
            }
            catch (AlreadyExitsError err)
            {
                throw new ArgumentException($"{err.Message} is already exists");
            }
        }

        private void AddOrSetImpl(ReadOnlySpan<string> keys, object? data)
        {
            string key = keys[0];
            if (keys.Length >= 2)
            {
                TreeModify childModify;
                if (ModifyDescriptor.TryGetValue(key, out var oldValue))
                {
                    if (oldValue is TreeModify modify) childModify = modify;
                    else throw new AlreadyExitsError(key);
                }
                else
                {
                    childModify = new TreeModify();
                    ModifyDescriptor[key] = childModify;
                }

                try
                {
                    childModify.AddOrSetImpl(keys.Slice(1), data);
                }
                catch (AlreadyExitsError e)
                {
                    throw new AlreadyExitsError($"{e.Message}.{key}");
                }
            }
            else
            {
                if (ModifyDescriptor.ContainsKey(key))
                    throw new AlreadyExitsError($"{key} is already exists");
                ModifyDescriptor[key] = data;
            }
        }

        private class AlreadyExitsError : Exception
        {
            public AlreadyExitsError(string? message) : base(message)
            {
            }
        }
    }
}
