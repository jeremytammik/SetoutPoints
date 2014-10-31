#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace SetoutPoints
{
  class CmdData
  {
    public CmdData( 
      string name, 
      string text, 
      string tip )
    {
      Name = name;
      Text = text;
      Tip = tip;
    }
    public string Name { get; set; }
    public string Text { get; set; }
    public string Tip { get; set; }
  }

  class App : IExternalApplication
  {
    const string Caption = "Setout Points";
    const string _class_name_prefix = "SetoutPoints.Cmd";

    //const string _cmd1 = "Mark Concrete Corners";
    //const string _cmd2 = "Renumber";

    static CmdData[] data = new CmdData[] {

      new CmdData( 
        "GeomVertices", 
        "Mark Concrete Corners",
        "Place a setout point marker on every concrete corner." ),

      new CmdData( 
        "Renumber", 
        "Renumber major", 
        "Renumber major setout points" )
    };

    public Result OnStartup( 
      UIControlledApplication a )
    {
      string path = System.Reflection.Assembly
        .GetExecutingAssembly().Location;

      // Create ribbon panel

      RibbonPanel p = a.CreateRibbonPanel( Caption );

      // Create buttons

      //PushButtonData d = new PushButtonData( 
      //  _cmd1, _cmd1, path, 
      //  _class_name_prefix + "GeomVertices" );

      //d.ToolTip = "Place a setout point marker "
      //  + "on every concrete corner.";

      //p.AddItem( d );

      //d = new PushButtonData( _cmd2, _cmd2, path, 
      //  _class_name_prefix + _cmd2 );

      //d.ToolTip = "Renumber major setout points";

      //p.AddItem( d );

      List<PushButtonData> buttonData 
        = new List<PushButtonData>( 
          data.Length );

      foreach( CmdData cd in data )
      {
        PushButtonData pbd = new PushButtonData(
          cd.Name, cd.Text, path,
          _class_name_prefix + cd.Name );

        pbd.ToolTip = cd.Tip;

        //p.AddItem( pbd );

        buttonData.Add( pbd );
      }

      p.AddStackedItems( buttonData[0], 
        buttonData[1] );

      return Result.Succeeded;
    }

    public Result OnShutdown( 
      UIControlledApplication a )
    {
      return Result.Succeeded;
    }
  }
}
