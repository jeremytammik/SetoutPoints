#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
#endregion

/// <summary>
/// Modified version of CmdGeomVertices, by Jan Grenov, in
/// http://forums.autodesk.com/t5/revit-api-forum/how-to-get-a-solids-location-in-the-project/m-p/6851892
/// The function GetCorners has been slightly modified 
/// and some methods are omitted in this version.
/// The GetRayPoints function has been added.
/// </summary>
namespace SetoutPoints
{
  class GeomVertices
  {
    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }

    /// <summary>
    /// Return a string for an XYZ point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( XYZ p )
    {
      return string.Format( "({0},{1},{2})",
        RealString( p.X ),
        RealString( p.Y ),
        RealString( p.Z ) );
    }

    /// <summary>
    /// Define equality for Revit XYZ points.
    /// Very rough tolerance, as used by Revit itself.
    /// </summary>
    class XyzEqualityComparer : IEqualityComparer<XYZ>
    {
      const double _sixteenthInchInFeet
        = 1.0 / ( 16.0 * 12.0 );

      public bool Equals( XYZ p, XYZ q )
      {
        return p.IsAlmostEqualTo( q,
          _sixteenthInchInFeet );
      }

      public int GetHashCode( XYZ p )
      {
        return PointString( p ).GetHashCode();
      }
    }

    /// <summary>
    /// Return all the "corner" vertices of a given solid.
    /// Note that a circle in Revit consists of two arcs
    /// and will return a "corner" at each of the two arc
    /// end points.
    /// </summary>
    public static Dictionary<XYZ, int> GetCorners( List<Solid> solids )
    {
      Dictionary<XYZ, int> corners
        = new Dictionary<XYZ, int>(
          new XyzEqualityComparer() );

      foreach( Solid solid in solids )
      {
        foreach( Face f in solid.Faces )
        {
          foreach( EdgeArray ea in f.EdgeLoops )
          {
            foreach( Edge e in ea )
            {
              XYZ p = e.AsCurveFollowingFace( f )
                .GetEndPoint( 0 );

              if( !corners.ContainsKey( p ) )
              {
                corners[p] = 0;
              }
              ++corners[p];
            }
          }
        }
      }

      return corners;
    }

    public static BboxInfo GetSolidsBoundingBoxInfo( 
      Dictionary<XYZ, int> dict, 
      ref Transform t )
    {
      double minX = double.MaxValue;
      double minY = double.MaxValue;
      double minZ = double.MaxValue;
      double maxX = double.MinValue;
      double maxY = double.MinValue;
      double maxZ = double.MinValue;

      foreach( XYZ p in dict.Keys )
      {
        if( p.X < minX )
        {
          minX = p.X;
        }
        if( p.Y < minY )
        {
          minY = p.Y;
        }
        if( p.Z < minZ )
        {
          minZ = p.Z;
        }
        if( p.X > maxX )
        {
          maxX = p.X;
        }
        if( p.Y > maxY )
        {
          maxY = p.Y;
        }
        if( p.Z > maxZ )
        {
          maxZ = p.Z;
        }
      }

      XYZ centerPoint = new XYZ( minX + ( ( maxX - minX ) / 2 ), minY + ( ( maxY - minY ) / 2 ), minZ + ( ( maxZ - minZ ) / 2 ) );
      double halfWidth = Math.Abs( maxX - minX ) / 2;
      double halfDepth = Math.Abs( maxY - minY ) / 2;
      double halfHeight = Math.Abs( maxZ - minZ ) / 2;


      centerPoint = t.OfPoint( centerPoint );

      BboxInfo bbInfo = new BboxInfo( centerPoint, halfWidth, halfDepth, halfHeight );

      return bbInfo;

    }

    public static List<Solid> GetSolids( Element e, Options opt, out Transform t )
    {
      GeometryElement geo = e.get_Geometry( opt );

      List<Solid> solids = new List<Solid>();
      GeometryInstance inst = null;
      t = Transform.Identity;

      // Some columns have no solids, and we have to 
      // retrieve the geometry from the symbol; 
      // others do have solids on the instance itself 
      // and no contents in the instance geometry 
      // (e.g. in rst_basic_sample_project.rvt).

      foreach( GeometryObject obj in geo )
      {
        Solid solid = obj as Solid;

        if( null != solid && 0 < solid.Faces.Size )
        {
          solids.Add( solid );
          //break;
        }

        inst = obj as GeometryInstance;
      }

      if( solids.Count == 0 && null != inst )
      {
        geo = inst.GetSymbolGeometry();
        t = inst.Transform;

        foreach( GeometryObject obj in geo )
        {
          Solid solid = obj as Solid;
          if( null != solid && 0 < solid.Faces.Size )
          {
            solids.Add( solid );
            //break;
          }
        }
      }
      return solids;
    }

  }

  public class BboxInfo
  {

    private XYZ _centerPoint;
    private double _halfwidth;
    private double _halfDepth;
    private double _halfHeight;


    public BboxInfo( XYZ point, double halfWidth, double halfDepth, double halfHeight )
    {
      _centerPoint = point;
      _halfwidth = halfWidth;
      _halfDepth = halfDepth;
      _halfHeight = halfHeight;
    }

    public XYZ CenterPoint
    {
      get
      {
        return _centerPoint;
      }
    }

    public double HalfWidth
    {
      get
      {
        return _halfwidth;
      }
    }

    public double HalfDepth
    {
      get
      {
        return _halfDepth;
      }
    }

    public double HalfHeight
    {
      get
      {
        return _halfHeight;
      }
    }
  }
}
