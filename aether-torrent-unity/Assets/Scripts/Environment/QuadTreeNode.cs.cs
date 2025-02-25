using System.Collections.Generic;
using UnityEngine;

public class QuadTreeNode
{
    public Bounds MBounds;
    List<QuadTreeNode> children = new List<QuadTreeNode>();
    bool octree;
    List<Matrix4x4> positionsHeld = new List<Matrix4x4>();

    public QuadTreeNode(Bounds bounds, int depth, bool octree)
    {
        MBounds = bounds;
        this.octree = octree;
        children.Clear();
        // only build a new node is the depth is over 0
        if (depth > 0)
        {
            // examples for octree or quadtree
            if (this.octree)
            {
                BuildOcTree(depth);
            }
            else
            {
                BuildQuadTree(depth);
            }
        }
    }


    public void RetrieveAllLeaves(List<Bounds> list)
    {
        // find all leaf (childless) nodes for debug visuals
        if (children.Count == 0)
        {
            list.Add(MBounds);
        }
        else
        {
            foreach (var child in children)
            {
                child.RetrieveAllLeaves(list);
            }
        }
    }


    void BuildQuadTree(int depth)
    {
        // build a quadtree
        // size/4 for offsetting positions for childrens bounds centers
        var size = MBounds.size;
        size /= 4.0f;
        // size/2 for childrens bounds size
        var childSize = MBounds.size / 2.0f;
        // y size is total size because we dont subdivide in that axis
        childSize.y = MBounds.size.y;
        var center = MBounds.center;

        // create bounds for every corner, only in x and z 
        var topLeft = new Bounds(new Vector3(center.x - size.x, center.y, center.z - size.z), childSize);
        var bottomRight = new Bounds(new Vector3(center.x + size.x, center.y, center.z + size.z), childSize);
        var topRight = new Bounds(new Vector3(center.x - size.x, center.y, center.z + size.z), childSize);
        var bottomLeft = new Bounds(new Vector3(center.x + size.x, center.y, center.z - size.z), childSize);

        // add children by creating a new node
        children.Add(new QuadTreeNode(topLeft, depth - 1, octree));
        children.Add(new QuadTreeNode(bottomRight, depth - 1, octree));
        children.Add(new QuadTreeNode(topRight, depth - 1, octree));
        children.Add(new QuadTreeNode(bottomLeft, depth - 1, octree));
    }

    void BuildOcTree(int depth)
    {
        // build a quadtree
        // size/4 for offsetting positions for childrens bounds centers
        var size = MBounds.size;
        size /= 4.0f;
        // size/2 for childrens bounds size
        var childSize = MBounds.size / 2.0f;
        var center = MBounds.center;

        // layer 1, negative y axis offset
        var topLeft = new Bounds(new Vector3(center.x - size.x, center.y - size.y, center.z - size.z), childSize);
        var bottomRight = new Bounds(new Vector3(center.x + size.x, center.y - size.y, center.z + size.z), childSize);
        var topRight = new Bounds(new Vector3(center.x - size.x, center.y - size.y, center.z + size.z), childSize);
        var bottomLeft = new Bounds(new Vector3(center.x + size.x, center.y - size.y, center.z - size.z), childSize);

        // layer 2, positive y axis offset
        var topLeft2 = new Bounds(new Vector3(center.x - size.x, center.y + size.y, center.z - size.z), childSize);
        var bottomRight2 = new Bounds(new Vector3(center.x + size.x, center.y + size.y, center.z + size.z), childSize);
        var topRight2 = new Bounds(new Vector3(center.x - size.x, center.y + size.y, center.z + size.z), childSize);
        var bottomLeft2 = new Bounds(new Vector3(center.x + size.x, center.y + size.y, center.z - size.z), childSize);

        // add both layers of children by creating a new node for each
        children.Add(new QuadTreeNode(topLeft, depth - 1, octree));
        children.Add(new QuadTreeNode(bottomRight, depth - 1, octree));
        children.Add(new QuadTreeNode(topRight, depth - 1, octree));
        children.Add(new QuadTreeNode(bottomLeft, depth - 1, octree));

        children.Add(new QuadTreeNode(topLeft2, depth - 1, octree));
        children.Add(new QuadTreeNode(bottomRight2, depth - 1, octree));
        children.Add(new QuadTreeNode(topRight2, depth - 1, octree));
        children.Add(new QuadTreeNode(bottomLeft2, depth - 1, octree));
    }

    public void RetrieveLeaves(Plane[] frustum, List<Bounds> boundsList, List<Matrix4x4> visibleMatrixList)
    {
        // check if frustum is overlapping with bounds
        if (GeometryUtility.TestPlanesAABB(frustum, MBounds))
        {
            // if we are a leaf node, add the list of matrices to the combining list in culllinginstanceddemo
            if (children.Count == 0)
            {
                visibleMatrixList.AddRange(positionsHeld);
                boundsList.Add(MBounds);
            }
            // if we have children, check those
            else
            {
                foreach (var child in children)
                {
                    child.RetrieveLeaves(frustum, boundsList, visibleMatrixList);
                }
            }
        }
    }

    public void FindLeafForPoint(Matrix4x4 point)
    {
        // check if matrix4x4 point is inside of the bounds of this node
        if (MBounds.Contains(point.GetColumn(3)))
        {
            // if we are a leaf node, add the point to the list this leaf holds
            if (children.Count == 0)
            {
                positionsHeld.Add(point);
            }
            // if we have children, check those
            else
            {
                foreach (var child in children)
                {
                    child.FindLeafForPoint(point);
                }
            }
        }
    }

    public bool ClearEmpty()
    {
        // if the node is empty, we can safely delete it
        var delete = false;
        if (children.Count > 0)
        {
            // dont delete things from a list when iterating forward, because you can miss items, so we are iterating backwards here
            var i = children.Count;
            while (i > 0)
            {
                i--;
                if (children[i].ClearEmpty())
                {
                    children.RemoveAt(i);
                }
            }
        }
        // if its empty and a leaf node, return true
        if (positionsHeld.Count == 0 && children.Count == 0)
        {
            delete = true;
        }
        return delete;
    }


}