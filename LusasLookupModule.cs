//*******************************************************************
// LusasLookupModule.cs
// Author: Apostolos Grammatopoulos
//
// LusasLookupModule class implementation file
//*******************************************************************

using KSharedEnums;
using Lusas.LPI;
using Lusas.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LusasLookup
{
    /// <summary>COM Interface exposing module functionality</summary>
    [ComVisible(true)]
    [Guid("32a00e71-25e9-4a0d-9dbf-c8149b010df6"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ILusasLookupModule {}

    /// <summaryThe main LusasLookup module class that interoperates with Modeller.</summary>
    [ComVisible(true)]
    [Guid("d1d84060-f8e5-47b9-8be6-648e0c1709f9"), ClassInterface(ClassInterfaceType.None)]
    public class LusasLookupModule : LusasModuleClass, ILusasLookupModule
    {
        #region Menu Event Handlers

        private long m_menuID;      // Temporary menu item ID 

        /// <summary>Called when Modeller is redrawing the Main Menu.</summary>
        /// <remarks>
        /// Perform temporary dev-only customisation to Modeller's main menu here.
        /// Note - modules intended for release should NOT use this method to add themselves into
        /// modeller's menu - instead the menu item should be properly and consistently added into
        /// LUSASres.rc and the class should override onMenuUpdate() and onMenuClick()
        /// </remarks>
        protected override void onRefreshMainMenu()
        {
            // Create a default [Modules] > [Module Name] menu for development.
            IFMenu rootMenu = Modeller.getMainMenu();
            IFMenu modMenu;
            if (rootMenu.exists("Modules"))
            {
                modMenu = rootMenu.getSubMenu("Modules");
            }
            else
            {
                modMenu = rootMenu.appendMenu("Modules");
            }
            m_menuID = modMenu.appendItem("LusasLookup", @"Display ""LusasLookup"" Dialog");
        }

        /// <summary>Called when the user clicks on a menu entry.</summary>
        /// <param name="menuID">ID of the menu that has been clicked.</param>
        /// <param name="editingObj">Object that is being edited (nothing when creating a new object).</param>
        /// <param name="clientData">Data that was provided to Modeller when defining edittingObj.</param>
        /// <returns>true if the click event was handled by this Module.</returns>
        /// <remarks>LUSAS expects the a Module handling the event to execute itself (typically using runModule()).</remarks>
        protected override bool onMenuClick(int menuID, object editingObj, object clientData = null)
        {
            if (m_menuID == menuID)
            {
                try
                {
                    using (var dlg = new LusasLookupDialog(this))
                    {
                        dlg.ShowDialog();
                    }
                }
                catch (Exception ex){
                    Modeller.AfxMsgBox(ex.Message, TextOutStyles.textOutError_E);
                }
                return true; // Handled the menu event
            }
            return false; // Allow others to handle the event
        }


        /// <summary>
        /// Called when a menu entry needs to be drawn.
        /// Allows the Module to specify whether the menu item should be disabled or checked.
        /// </summary>
        /// <param name="menuID">ID of the menu that has been clicked.</param>
        /// <param name="edittingObj">Object that is being edited (nothing when creating a new object).</param>
        /// <param name="enable">Set to true to enable the menu item.</param>
        /// <param name="isChecked">
        /// Set to 0 to show an 'off' tickbox next to the menu.
        /// Set to 1 to show an 'on' tick mark by the side of the menu.
        /// Set to 2 to show an indeterminate check.
        /// Set to 3 to show no tick at all.
        /// </param>
        /// <param name="clientData">Data that was provided to Modeller when defining edittingObj.</param>
        /// <returns>true if the update event was handled by this Module.</returns>
        /// <remarks>
        /// Only when a Module handles an menu update event are the changed values of enable/checked respected.
        /// </remarks>
        protected override bool onMenuUpdate(int menuID, object editingObj, ref bool enable, ref int isChecked, object clientData = null)
        {
            if (m_menuID == menuID)
            {
                enable = true;
                return true; // Handled the menu event
            }
            return false; // Allow others to handle the event
        }

        #endregion
    }

}
