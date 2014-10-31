#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace SetoutPoints
{
  /// <summary>
  /// Renumber key setout points. We add a prefix
  /// "SOP " (defined by the static class variable 
  /// _sop_prefix) and restart numbering from one.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  public class CmdRenumber : IExternalCommand
  {
    const string _sop_prefix = "SOP ";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      FamilySymbol[] symbols
        = CmdGeomVertices.GetFamilySymbols( 
          doc, false );

      if( null == symbols )
      {
        TaskDialog.Show( "Setout Points",
          "Setout point family not loaded, "
          + "so no setout points present." );

        return Result.Succeeded;
      }

      // Filter for key setout points.
      // They are family instances with the generic 
      // model category whose Key_Setout_Point
      // shared parameter is set to true.
      // To get the key points only, we initially set 
      // up a parameter filter. For that, we need a
      // parameter definition to set up the parameter 
      // filter.
      // Later, we decided to switch type when we 
      // promote a setout point to a major or key 
      // point, so there is no need to filter for 
      // a parameter value at all; we can just 
      // filter for the major setout point type
      // instead.

      //FamilySymbolFilter symbolFilter 
      //  = new FamilySymbolFilter( symbol.Family.Id );

      //Element e
      //  = new FilteredElementCollector( doc )
      //    .WherePasses( symbolFilter )
      //    .FirstElement();

      //LogicalOrFilter f = new LogicalOrFilter(
      //  new FamilyInstanceFilter( doc, symbols[0].Id ),
      //  new FamilyInstanceFilter( doc, symbols[1].Id ) );

      FamilyInstanceFilter instanceFilter
        = new FamilyInstanceFilter( doc, symbols[0].Id );

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .OfCategory( BuiltInCategory.OST_GenericModel )
          .OfClass( typeof( FamilyInstance ) )
          .WherePasses( instanceFilter );

      //if( null == e )
      //{
      //  TaskDialog.Show( "Setout Points",
      //    "No key setout point found. " );
      //  return Result.Succeeded;
      //}

      //Parameter shared_parameter
      //  = e.get_Parameter( Command._parameter_key );
      //ParameterValueProvider provider
      //  = new ParameterValueProvider( 
      //    shared_parameter.Id );
      //FilterNumericRuleEvaluator evaluator
      //  = new FilterNumericEquals();
      //FilterRule rule
      //  = new FilterIntegerRule( 
      //    provider, evaluator, 1 );
      //ElementFilter paramFilter
      //  = new ElementParameterFilter( rule );
      //FilteredElementCollector col
      //  = new FilteredElementCollector( doc )
      //    .WherePasses( symbolFilter )
      //    .WherePasses( paramFilter );

      Guid guid = CmdGeomVertices._parameter_point_nr;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Renumber Setout Points" );

        int i = 0;
        string s;

        foreach( Element p in col )
        {
          s = _sop_prefix + ( ++i ).ToString();

          p.get_Parameter( guid ).Set( s );
        }

        tx.Commit();
      }
      return Result.Succeeded;
    }
  }
}
