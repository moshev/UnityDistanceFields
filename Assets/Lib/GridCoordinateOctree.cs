using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public struct GridCoordinate : IComparable<GridCoordinate>
{
    public int i, j, k;

    public GridCoordinate(int i, int j, int k)
    {
        this.i = i;
        this.j = j;
        this.k = k;
    }

    public int CompareTo(GridCoordinate other)
    {
        return k != other.k ? k.CompareTo(other.k) :
               j != other.j ? j.CompareTo(other.j) :
                              i.CompareTo(other.i);
    }

    public static GridCoordinate operator +(GridCoordinate a, GridCoordinate b)
    {
        return new GridCoordinate(a.i + b.i, a.j + b.j, a.k + b.k);
    }

    public static GridCoordinate operator -(GridCoordinate a, GridCoordinate b)
    {
        return new GridCoordinate(a.i - b.i, a.j - b.j, a.k - b.k);
    }

    public static GridCoordinate operator *(GridCoordinate a, int b)
    {
        return new GridCoordinate(a.i * b, a.j * b, a.k * b);
    }

    public static GridCoordinate operator *(int a, GridCoordinate b)
    {
        return b * a;
    }

    public static GridCoordinate operator /(GridCoordinate a, int b)
    {
        return new GridCoordinate(a.i / b, a.j / b, a.k / b);
    }

    public static bool operator ==(GridCoordinate a, GridCoordinate b)
    {
        return a.i == b.i && a.j == b.j && a.k == b.k;
    }

    public static bool operator !=(GridCoordinate a, GridCoordinate b)
    {
        return !(a == b);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(i, j, k);
    }

    public override string ToString()
    {
        return String.Format("[{0} {1} {2}]", i, j, k);
    }

    public override bool Equals(object obj)
    {
        if (obj is GridCoordinate)
        {
            return this == (GridCoordinate)obj;
        }
        else
        {
            return obj.Equals(this);
        }
    }

    public override int GetHashCode()
    {
        return i.GetHashCode() ^ j.GetHashCode() ^ k.GetHashCode();
    }
}

public struct GridEdge : IComparable<GridEdge>
{
    //! Negative vertex
    public GridCoordinate c0;

    //! Positive vertex
    public GridCoordinate c1;

    //! Distance field at v0
    public float d0;

    //! Distance field at v1
    public float d1;

    //! t*v0+(1-t)*v1 = crossing point
    public float t;

    //! Normal at intersection
    public Vector3 normal;

    public GridEdge(GridCoordinate c0, GridCoordinate c1, float d0, float d1)
    {
        this.c0 = c0;
        this.c1 = c1;
        this.d0 = d0;
        this.d1 = d1;
        this.normal = Vector3.zero;
        t = (Math.Abs(d1 - d0) < 1e-6f) ? 0.5f : d1 / (d1 - d0);
    }

    public int CompareTo(GridEdge other)
    {
        int v0Cmp = c0.CompareTo(other.c0);
        return v0Cmp != 0 ? v0Cmp : c1.CompareTo(other.c1);
    }

    public override String ToString()
    {
        return c0 + "->" + c1;
    }
}

public struct IndexedVector3
{
    public Vector3 v;
    public int i;

    public static IndexedVector3 Create(Vector3 v)
    {
        IndexedVector3 result;
        result.v = v;
        result.i = -1;
        return result;
    }
}

// Maintains associations between a GridCoordinate and a T
public class GridCoordinateOctree<T> : IEnumerable<T>
{
    private struct Element
    {
        public GridCoordinate c;
        public T v;
    }

    private interface INode : IEnumerable<T>
    {
        // Get value at coordinate c; return true if found; false otherwise
        bool Get(ref T v, GridCoordinate c);

        // Get node matching predicate (there can be multiple nodes per coordinate)
        bool Get(ref T v, GridCoordinate c, Predicate<T> predicate);

        // Add v at c; return a new INode if split was necessary
        INode Add(T v, GridCoordinate c);

        // Transform each value in-place
        void Map(Func<T, T> func);
    }

    private class LeafNode : INode
    {
        private Element[] elements;// = new Element[8];
        private int count = 0;
        private GridCoordinate min;
        private GridCoordinate max;

        public LeafNode(GridCoordinate min, GridCoordinate max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Get(ref T v, GridCoordinate c)
        {
            for (int i = 0; i < count; i++)
            {
                if (elements[i].c == c)
                {
                    v = elements[i].v;
                    return true;
                }
            }
            return false;
        }

        public bool Get(ref T v, GridCoordinate c, Predicate<T> predicate)
        {
            for (int i = 0; i < count; i++)
            {
                if (elements[i].c == c && predicate(elements[i].v))
                {
                    v = elements[i].v;
                    return true;
                }
            }
            return false;
        }

        public INode Add(T v, GridCoordinate c)
        {
            if (count == 0) elements = new Element[8];
            if (count < elements.Length)
            {
                Element e = new Element();
                e.c = c;
                e.v = v;
                elements[count++] = e;
                return this;
            }
            else
            {
                GridCoordinate diff = (max - min);
                if (diff.i < 2 && diff.j < 2 && diff.k < 2)
                {
                    // in this case we'll have nodes that are just too small
                    Debug.Log("Error trying to split node! Size is " + diff);
                    return this;
                }
                INode split = new ParentNode(min, max);
                for (int i = 0; i < elements.Length; i++)
                {
                    Element e = elements[i];
                    split = split.Add(e.v, e.c);
                }
                return split.Add(v, c);
            }
        }

        public void Map(Func<T, T> func)
        {
            for (int i = 0; i < count; i++)
            {
                elements[i].v = func(elements[i].v);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return elements[i].v;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class ParentNode : INode
    {
        private INode[] children = new INode[8];
        private GridCoordinate min;
        private GridCoordinate max;

        public ParentNode(GridCoordinate min, GridCoordinate max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Get(ref T v, GridCoordinate c)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                return false;
            }
            return children[n].Get(ref v, c);
        }

        public bool Get(ref T v, GridCoordinate c, Predicate<T> predicate)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                return false;
            }
            return children[n].Get(ref v, c, predicate);
        }

        public INode Add(T v, GridCoordinate c)
        {
            GridCoordinate mid = (min + max) / 2;
            int n = 0;
            if (c.i >= mid.i) n |= 1;
            if (c.j >= mid.j) n |= 2;
            if (c.k >= mid.k) n |= 4;
            if (children[n] == null)
            {
                GridCoordinate[] corners = new GridCoordinate[3] { min, mid, max };
                GridCoordinate nmin, nmax;
                nmin.i = corners[(n & 1) / 1 + 0].i;
                nmax.i = corners[(n & 1) / 1 + 1].i;
                nmin.j = corners[(n & 2) / 2 + 0].j;
                nmax.j = corners[(n & 2) / 2 + 1].j;
                nmin.k = corners[(n & 4) / 4 + 0].k;
                nmax.k = corners[(n & 4) / 4 + 1].k;
                children[n] = new LeafNode(nmin, nmax);
            }
            children[n] = children[n].Add(v, c);
            return this;
        }

        public void Map(Func<T, T> func)
        {
            for (int n = 0; n < 8; n++)
            {
                if (children[n] == null) continue;
                children[n].Map(func);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int n = 0; n < 8; n++)
            {
                if (children[n] == null) continue;
                foreach (T v in children[n])
                {
                    yield return v;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private int count = 0;
    private INode rootNode;
    public int Count { get { return count; } }

    public GridCoordinateOctree(GridCoordinate min, GridCoordinate max)
    {
        rootNode = new LeafNode(min, max);
    }

    // Get value at coordinate c; return true if found; false otherwise
    public bool Get(ref T v, GridCoordinate c)
    {
        return rootNode.Get(ref v, c);
    }

    // Get node matching predicate (there can be multiple nodes per coordinate)
    public bool Get(ref T v, GridCoordinate c, Predicate<T> predicate)
    {
        return rootNode.Get(ref v, c, predicate);
    }

    // Add v at c; return a new INode if split was necessary
    public void Add(T v, GridCoordinate c)
    {
        rootNode = rootNode.Add(v, c);
        count++;
    }

    // Transform each value in-place
    public void Map(Func<T, T> func)
    {
        rootNode.Map(func);
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (T v in rootNode)
        {
            yield return v;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}