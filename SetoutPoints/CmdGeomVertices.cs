#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion

namespace SetoutPoints
{
  [Transaction( TransactionMode.Manual )]
  public class CmdGeomVertices : IExternalCommand
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
    /// Classify the possible setup point 
    /// host geometry types that we support.
    /// </summary>
    enum HostType
    {
      Floors,
      Ramps,
      StructuralColumns,
      StructuralFraming,
      StructuralFoundations,
      Walls
    }

    /// <summary>
    /// Return the HostType for a given element.
    /// </summary>
    HostType GetHostType( Element e )
    {
      if( e is ContFooting )
      {
        return HostType.StructuralFoundations;
      }
      if( e is Floor )
      {
        return HostType.Floors;
      }
      if( e is Wall )
      {
        return HostType.Walls;
      }
      if( null != e.Category )
      {
        switch( e.Category.Id.IntegerValue )
        {
          case (int) BuiltInCategory.OST_StructuralColumns: return HostType.StructuralColumns;
          case (int) BuiltInCategory.OST_StructuralFraming: return HostType.StructuralFraming;
          case (int) BuiltInCategory.OST_StructuralFoundation: return HostType.StructuralFoundations;
          case (int) BuiltInCategory.OST_Floors: return HostType.Floors;
          case (int) BuiltInCategory.OST_Ramps: return HostType.Ramps;
        }
      }
      Debug.Assert( false, "what host type is this element?" );
      return HostType.Floors;
    }

    /// <summary>
    /// Return a string describing the given element:
    /// .NET type name, category name, family and 
    /// symbol name for a family instance, element id 
    /// and element name.
    /// </summary>
    public static string ElementDescription( Element e )
    {
      if( null == e )
      {
        return "<null>";
      }

      // For a wall, the element name equals the
      // wall type name, which is equivalent to the
      // family name ...

      FamilyInstance fi = e as FamilyInstance;

      string typeName = e.GetType().Name;

      string categoryName = ( null == e.Category )
        ? string.Empty
        : e.Category.Name + " ";

      string familyName = ( null == fi )
        ? string.Empty
        : fi.Symbol.Family.Name + " ";

      string symbolName = ( null == fi
        || e.Name.Equals( fi.Symbol.Name ) )
          ? string.Empty
          : fi.Symbol.Name + " ";

      return string.Format( "{0} {1}{2}{3}<{4} {5}>",
        typeName, categoryName, familyName, symbolName,
        e.Id.IntegerValue, e.Name );
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
    Dictionary<XYZ,int> GetCorners( Solid solid )
    {
      Dictionary<XYZ, int> corners 
        = new Dictionary<XYZ, int>( 
          new XyzEqualityComparer() );

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
      return corners;
    }

    /// <summary>
    /// Retrieve all structural elements that we are 
    /// interested in using to define setout points.
    /// We are looking at concrete for the moment.
    /// This includes: columns, framing, floors, 
    /// foundations, ramps, walls. 
    /// </summary>
    FilteredElementCollector GetStructuralElements(
      Document doc )
    {
      // What categories of family instances
      // are we interested in?

      BuiltInCategory[] bics = new BuiltInCategory[] {
        BuiltInCategory.OST_StructuralColumns,
        BuiltInCategory.OST_StructuralFraming,
        BuiltInCategory.OST_StructuralFoundation,
        BuiltInCategory.OST_Floors,
        BuiltInCategory.OST_Ramps
      };

      IList<ElementFilter> a
        = new List<ElementFilter>( bics.Length );

      foreach( BuiltInCategory bic in bics )
      {
        a.Add( new ElementCategoryFilter( bic ) );
      }

      LogicalOrFilter categoryFilter
        = new LogicalOrFilter( a );

      // Filter only for structural family 
      // instances using concrete or precast 
      // concrete structural material:

      List<ElementFilter> b
        = new List<ElementFilter>( 2 );

      b.Add( new StructuralMaterialTypeFilter( 
        StructuralMaterialType.Concrete ) );

      b.Add( new StructuralMaterialTypeFilter( 
        StructuralMaterialType.PrecastConcrete ) );

      LogicalOrFilter structuralMaterialFilter 
        = new LogicalOrFilter( b );

      List<ElementFilter> c
        = new List<ElementFilter>( 3 );

      c.Add( new ElementClassFilter( 
        typeof( FamilyInstance ) ) );

      c.Add( structuralMaterialFilter );
      c.Add( categoryFilter );

      LogicalAndFilter familyInstanceFilter
        = new LogicalAndFilter( c );

      IList<ElementFilter> d
        = new List<ElementFilter>( 6 );

      d.Add( new ElementClassFilter(
        typeof( Wall ) ) );

      d.Add( new ElementClassFilter(
        typeof( Floor ) ) );

      //d.Add( new ElementClassFilter(
      //  typeof( ContFooting ) ) );

#if NEED_LOADS
      d.Add( new ElementClassFilter(
        typeof( PointLoad ) ) );

      d.Add( new ElementClassFilter(
        typeof( LineLoad ) ) );

      d.Add( new ElementClassFilter(
        typeof( AreaLoad ) ) );
#endif

      d.Add( familyInstanceFilter );

      LogicalOrFilter classFilter
        = new LogicalOrFilter( d );

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WherePasses( classFilter );

      return col;
    }

    #region Setout point family identification constants

    public const string FamilyName = "SetoutPoint";
 
    public const string SymbolName = "SetoutPoint";
 
    const string _extension = ".rfa";
 
    const string _directory = 
      // "C:/a/doc/revit/blog/src/SetoutPoints/test/"; // 2013
      "Z:/a/doc/revit/blog/src/SetoutPoints/test/"; // 2015
 
    const string _family_path
      = _directory + FamilyName + _extension;

    // Shared parameter GUIDs retrieved 
    // from the shared parameter file.

    static Guid _parameter_host_type = new Guid( "27188736-2491-4ac8-b634-8f4c9399afef" );
    static Guid _parameter_host_id = new Guid( "64221c53-558b-4f29-a469-039a2001a037" );
    public static Guid _parameter_key = new Guid( "f48cd131-b9b6-432a-8b5c-a7534183a880" );
    public static Guid _parameter_point_nr = new Guid( "febfe8b9-6938-4099-8cf6-d62f58a9c933" );
    static Guid _parameter_x = new Guid( "7a5d1056-a1df-4389-b026-9f32fc3ac5fb" );
    static Guid _parameter_y = new Guid( "84f9a2be-85d5-44da-94b9-fc5b7808026b" );
    static Guid _parameter_z = new Guid( "04c33d6a-f7f1-450c-8b15-9ac9aba24606" );

    #endregion // Setout point family identification constants

    /// <summary>
    /// Retrieve project base point.
    /// </summary>
    bool GetBasePoint( 
      Document doc, 
      out XYZ basePoint, 
      out double north )
    {
      BuiltInParameter [] bip = new [] {
        BuiltInParameter.BASEPOINT_EASTWEST_PARAM,
        BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM,
        BuiltInParameter.BASEPOINT_ELEVATION_PARAM,
        BuiltInParameter.BASEPOINT_ANGLETON_PARAM
      };

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .OfClass( typeof( BasePoint ) );

      Parameter p = null;
      basePoint = null;
      north = 0;

      foreach( BasePoint bp in col )
      {
        basePoint = new XYZ(
          bp.get_Parameter( bip[0] ).AsDouble(),
          bp.get_Parameter( bip[1] ).AsDouble(),
          bp.get_Parameter( bip[2] ).AsDouble() );

        Debug.Print( "base point {0}",
          PointString( basePoint ) );

        p = bp.get_Parameter( bip[3] );

        if( null != p )
        {
          north = p.AsDouble();
          Debug.Print( "north {0}", north );
          break;
        }
      }
      return null != p;
    }

    /// <summary>
    /// Return the project location transform.
    /// </summary>
    Transform GetProjectLocationTransform( Document doc )
    {
      // Retrieve the active project location position.

      ProjectPosition projectPosition
        = doc.ActiveProjectLocation.get_ProjectPosition(
          XYZ.Zero );

      // Create a translation vector for the offsets

      XYZ translationVector = new XYZ(
        projectPosition.EastWest,
        projectPosition.NorthSouth,
        projectPosition.Elevation );

      Transform translationTransform
        = Transform.CreateTranslation(
          translationVector );

      // Create a rotation for the angle about true north

      //Transform rotationTransform
      //  = Transform.get_Rotation( XYZ.Zero,
      //    XYZ.BasisZ, projectPosition.Angle );

      Transform rotationTransform 
        = Transform.CreateRotation(
          XYZ.BasisZ, projectPosition.Angle );

      // Combine the transforms 

      Transform finalTransform
        = translationTransform.Multiply(
          rotationTransform );

      return finalTransform;
    }

    /// <summary>
    /// Retrieve or load the setout point family symbols.
    /// </summary>
    public static FamilySymbol [] GetFamilySymbols( 
      Document doc, 
      bool loadIt )
    {
      FamilySymbol [] symbols = null;

      Family family 
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Family ) )
          .FirstOrDefault<Element>( 
            e => e.Name.Equals( FamilyName ) ) 
          as Family;

      // If the family is not already loaded, do so:

      if( null == family && loadIt )
      {
        // Load the setout point family

        using( Transaction tx = new Transaction( 
          doc ) )
        {
          tx.Start( "Load Setout Point Family" );

          //if( !doc.LoadFamilySymbol(
          //  _family_path, SymbolName, out symbol ) )

          if( doc.LoadFamily( _family_path, 
            out family ) )
          {
            tx.Commit();
          }
          else
          {
            tx.RollBack();
          }
        }
      }

      if( null != family )
      {
        symbols = new FamilySymbol[2];

        int i = 0;

        //foreach( FamilySymbol s in family.Symbols ) // 2014

        foreach( ElementId id in family
          .GetFamilySymbolIds() ) // 2015
        {
          symbols[i++] = doc.GetElement(id) 
            as FamilySymbol;
        }

        Debug.Assert( 
          symbols[0].Name.EndsWith( "Major" ), 
          "expected major (key) setout point first" );

        Debug.Assert( 
          symbols[1].Name.EndsWith( "Minor" ), 
          "expected minor setout point second" );
      }
      return symbols;
    }

    /// <summary>
    /// Retrieve the first non-empty solid found for 
    /// the given element. In case the element is a 
    /// family instance, it may have its own non-empty
    /// solid, in which case we use that. Otherwise we 
    /// search the symbol geometry. If we use the 
    /// symbol geometry, we have to keep track of the 
    /// instance transform to map it to the actual
    /// instance project location.
    /// </summary>
    Solid GetSolid( Element e, Options opt )
    {
      GeometryElement geo = e.get_Geometry( opt );

      Solid solid = null;
      GeometryInstance inst = null;
      Transform t = Transform.Identity;

      // Some columns have no solids, and we have to 
      // retrieve the geometry from the symbol; 
      // others do have solids on the instance itself 
      // and no contents in the instance geometry 
      // (e.g. in rst_basic_sample_project.rvt).

      foreach( GeometryObject obj in geo )
      {
        solid = obj as Solid;

        if( null != solid
          && 0 < solid.Faces.Size )
        {
          break;
        }

        inst = obj as GeometryInstance;
      }

      if( null == solid && null != inst )
      {
        geo = inst.GetSymbolGeometry();
        t = inst.Transform;

        foreach( GeometryObject obj in geo )
        {
          solid = obj as Solid;

          if( null != solid
            && 0 < solid.Faces.Size )
          {
            break;
          }
        }
      }
      return solid;
    }

    //const double _feetToMm = 25.4 * 12.0;

    /// <summary>
    /// Setout point number, continues growing from
    /// one command launch to the next.
    /// </summary>
    static int _point_number = 0;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // Useless initial attempt to retrieve project 
      // location to calculate project location:
      //XYZ basePoint;
      //double north;
      //GetBasePoint( doc, out basePoint, out north );

      // Successful retrieval of active project 
      // location position as a transform:

      Transform projectLocationTransform 
        = GetProjectLocationTransform( doc );

      // Load or retrieve setout point family symbols:

      FamilySymbol [] symbols 
        = GetFamilySymbols( doc, true );

      if( null == symbols )
      {
        message = string.Format(
          "Unable to load setout point family from '{1}'.",
          _family_path );

        return Result.Failed;
      }

      // Retrieve structural concrete elements.

      FilteredElementCollector col 
        = GetStructuralElements( doc );

      // Retrieve element geometry and place a
      // setout point on each geometry corner.

      // Setout points are numbered starting at 
      // one each time the command is run with
      // no decoration or prefix whatsoever.

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Place Setout Points" );

        Options opt = app.Create.NewGeometryOptions();

        // On the very first attempt only, run an error 
        // check to see whether the required shared 
        // parameters have actually been bound:

        bool first = true;

        foreach( Element e in col )
        {
          Solid solid = GetSolid( e, opt );

          string desc = ElementDescription( e );

          if( null == solid )
          {
            Debug.Print(
              "Unable to access element solid for element {0}.",
              desc );

            continue;
          }

          Dictionary<XYZ, int> corners
            = GetCorners( solid );

          int n = corners.Count;

          Debug.Print( "{0}: {1} corners found:", desc, n );

          foreach( XYZ p in corners.Keys )
          {
            ++_point_number;

            Debug.Print( "  {0}: {1}",
              _point_number, PointString( p ) );

            FamilyInstance fi
              = doc.Create.NewFamilyInstance( p,
                symbols[1], StructuralType.NonStructural );

            XYZ p1 = t.OfPoint( p );

            FamilyInstance fi 
              = doc.Create.NewFamilyInstance( p1, 
                symbols[1], StructuralType.NonStructural );

            #region Test shared parameter availability
    #if TEST_SHARED_PARAMETERS
            // Test code to ensure that the shared 
            // parameters really are available

            Parameter p1 = fi.get_Parameter( "X" );
            Parameter p2 = fi.get_Parameter( "Y" );
            Parameter p3 = fi.get_Parameter( "Z" );
            Parameter p4 = fi.get_Parameter( "Host_Geometry" );
            Parameter p5 = fi.get_Parameter( "Point_Number" );

            //doc.Regenerate(); // no need for this, thankfully

            //Parameter p11 = fi.get_Parameter( 
            //  "{7a5d1056-a1df-4389-b026-9f32fc3ac5fb}" );

            //Parameter p12 = fi.get_Parameter( 
            //  "7a5d1056-a1df-4389-b026-9f32fc3ac5fb" );
    #endif // TEST_SHARED_PARAMETERS
            #endregion // Test shared parameter availability

            // Add shared parameter data immediately 
            // after creating the new family instance.
            // The shared parameters are indeed added 
            // immediately by Revit, so we can access and
            // populate them.
            // No need to commit the transaction that 
            // added the family instance to give Revit 
            // a chance to add the shared parameters to 
            // it, nor to regenerate the document, we 
            // can write the shared parameter values 
            // right away.

            if( first )
            {
              Parameter q = fi.get_Parameter(
                _parameter_x );

              if( null == q )
              {
                message = 
                  "The required shared parameters "
                  + "X, Y, Z, Host_Id, Host_Type and "
                  + "Point_Number are missing.";

                tx.RollBack();

                return Result.Failed;
              }
              first = false;
            }

            // Transform insertion point by applying
            // base point offset, scaling from feet,
            // and rotating to project north.
            //XYZ r1 = ( p + basePoint) * _feetToMm;

            // Transform insertion point by applying
            // project location transformation.

            XYZ r2 = projectLocationTransform.OfPoint( p );

            fi.get_Parameter( _parameter_host_type ).Set(
              GetHostType( e ).ToString() );

            fi.get_Parameter( _parameter_host_id ).Set(
              e.Id.IntegerValue );

            fi.get_Parameter( _parameter_point_nr ).Set(
              _point_number.ToString() );

            fi.get_Parameter( _parameter_x ).Set( r2.X );
            fi.get_Parameter( _parameter_y ).Set( r2.Y );
            fi.get_Parameter( _parameter_z ).Set( r2.Z );
          }
        }

        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
