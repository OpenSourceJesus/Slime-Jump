using System;
using Extensions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class TreeNode<T> : IEnumerable<TreeNode<T>>
{
	public T value;
	public List<TreeNode<T>> children = new List<TreeNode<T>>();
	public TreeNode<T> parent;
	
	public TreeNode<T> this[int i]
	{
		get
		{
			return children[i];
		}
	}
	
	public TreeNode (T value)
	{
		this.value = value;
	}

	public TreeNode<T> AddChild (T value)
	{
		TreeNode<T> node = new TreeNode<T>(value) {parent = this};
		children.Add(node);
		return node;
	}

	public TreeNode<T>[] AddChildren (params T[] values)
	{
		return values.Select(AddChild).ToArray();
	}

	public bool RemoveChild (TreeNode<T> node)
	{
		return children.Remove(node);
	}

	public void Traverse (Action<T> action)
	{
		action(value);
		foreach (TreeNode<T> child in children)
			child.Traverse(action);
	}

	public IEnumerable<T> Flatten ()
	{
		return new[] { value }.Concat(children.SelectMany(x => x.Flatten()));
	}

	IEnumerator IEnumerable.GetEnumerator ()
	{
		return GetEnumerator();
	}

	public IEnumerator<TreeNode<T>> GetEnumerator ()
	{
		yield return this;
		foreach (TreeNode<T> directChild in this.children)
		{
			foreach (TreeNode<T> anyChild in directChild)
				yield return anyChild;
		}
	}

	public virtual TreeNode<T> GetRoot ()
	{
		TreeNode<T> root = this;
		while (root.parent != null)
			root = root.parent;
		return root;
	}

	public virtual bool Contains (T value)
	{
		TreeNode<T> root = this;
		IEnumerator rootEnumerator = root.GetEnumerator();
		TreeNode<T> node;
		while (rootEnumerator.MoveNext())
		{
			node = (TreeNode<T>) rootEnumerator.Current; 
			if (node.value.Equals(value))
				return true;
		}
		return false;
	}

	public virtual TreeNode<T> GetChild (T value)
	{
		TreeNode<T> root = this;
		IEnumerator rootEnumerator = root.GetEnumerator();
		TreeNode<T> node;
		while (rootEnumerator.MoveNext())
		{
			node = (TreeNode<T>) rootEnumerator.Current; 
			if (node.value.Equals(value))
				return node;
		}
		return null;
	}

	public int[] GetPathToChild (T value)
	{
		if (this.value.Equals(value))
			return new int[0];
		List<KeyValuePair<int[], TreeNode<T>>> remainingChildValuesAndPaths = new List<KeyValuePair<int[], TreeNode<T>>>();
		remainingChildValuesAndPaths.Add(new KeyValuePair<int[], TreeNode<T>>(new int[0], this));
		while (remainingChildValuesAndPaths.Count > 0)
		{
			KeyValuePair<int[], TreeNode<T>> firstRemainingChildValueAndPath = remainingChildValuesAndPaths[0];
			for (int i = 0; i < firstRemainingChildValueAndPath.Value.children.Count; i ++)
			{
				if (firstRemainingChildValueAndPath.Value.children[i].value.Equals(value))
					return firstRemainingChildValueAndPath.Key.Add(i);
				remainingChildValuesAndPaths.Add(new KeyValuePair<int[], TreeNode<T>>(firstRemainingChildValueAndPath.Key.Add(i), firstRemainingChildValueAndPath.Value.children[i]));
			}
			remainingChildValuesAndPaths.RemoveAt(0);
		}
		return null;
	}

	public virtual TreeNode<T> GetChildAtPath (int[] path)
	{
		TreeNode<T> output = this;
		for (int i = 0; i < path.Length; i ++)
		{
			if (output.children.Count > path[i])
				output = output.children[path[i]];
		}
		return output;
	}

	public virtual int GetMaxTiers ()
	{
		throw new Exception("Not implemented yet");
	}
}
