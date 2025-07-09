//*******************************************************************
// LusasLookupDialog.cs
// Author: Apostolos Grammatopoulos
//
// LusasLookupDialog class implementation file
//*******************************************************************

using Lusas.Common.Extensions;
using Lusas.LPI;
using Lusas.Module;
using Lusas.Utils.Interop;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LusasLookup
{
    /// <summary>Module dialog providing UI to access LusasLookupModule functionality</summary>
    public partial class LusasLookupDialog : LusasModuleDialog
    {
        private LusasLookupModule m_module; // Reference to the module 
        private IFModeller m_modeller; // Reference to Modeller

        /// <summary>Constructs an instance of the safeprojectnameModule dialog</summary>
        /// <param name="lusasModule"></param>
        public LusasLookupDialog(LusasLookupModule lusasModule) : base(lusasModule)
        {
            m_module = lusasModule;
            m_modeller = lusasModule.Modeller;

            InitializeComponent();

            // Check if model is open
            if (!m_modeller.databaseExists()) {
                m_modeller.AfxMsgBox("Please, open a model first.");
                return;
            }

            PopulateTreeView();

            // Find launch target
            long noOfSelected = m_modeller.getSelection().count("all");
            IFDatabaseMember targetObject;
            if (noOfSelected == 0)
            {
                // Nothing selected, target Visible or DB
                // Get visible
                targetObject = m_modeller.getVisibleSet();
                // If all are visible, then pass database
                if(m_modeller.db().count("all") == ((IFObjectSet)targetObject).count("all")) targetObject = m_modeller.db();
            } else if (noOfSelected == 1) {
                // 1 selected, target it
                targetObject = CastObject<IFDatabaseMember>.arrayFromArrayObject(m_modeller.getSelection().getObjects("all"))[0];
            } else {
                // Selected object is targeted
                targetObject = m_modeller.getSelection();
            }

            // Populate the DataGridView
            PopulateDataGridView(targetObject);

            // Handle the NodeMouseClick event to capture node clicks
            treeView.NodeMouseClick += TreeView_NodeMouseClick;
        }

        /// <summary>Populate table with object properties and methods</summary>
        /// <param name="targetObjs">Target object</param>
        private void PopulateDataGridView(object targetObjs)
        {
            // Clear current table
            dgvObjectMethods.Rows.Clear();
            txtViewObj.Text = "Loading...";
            this.Cursor = Cursors.WaitCursor;
            Type type = ComTypeHelper.GetCOMObjectType(targetObjs);
            var properties = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Update object Name
            var getNameMethod = methods.FirstOrDefault(m => m.Name == "getName");
            if (getNameMethod != null) {
                var objName = getNameMethod.Invoke(targetObjs, new object[getNameMethod.GetParameters().Length]);
                txtViewObj.Text = $"{objName} ({type.Name})";
            } else {
                txtViewObj.Text = $"Unnamed object ({type.Name})";
            }

            // Load properties
            foreach (var property in properties) {
                var value = property.GetValue(targetObjs);
                dgvObjectMethods.Rows.Add(property.Name, property.PropertyType.Name, value?.ToString() ?? "null");
            }

            // Load methods
            string[] typesToLoad = new string[]{ "Int32", "UInt32", "Int64", "UInt64", "Double", "String", "Boolean", "SByte" };
            MethodInfo getValueNamesMethod = null;
            MethodInfo getValueUnitsMethod = null;
            MethodInfo getValueMethod = null;
            foreach (var method in methods) {
                var methodName = method.Name + "()";
                var retType = method.ReturnType.Name;

                // Try to load some of the method returns
                if (typesToLoad.Contains(retType) && 
                    !methodName.StartsWith("delete") && !methodName.StartsWith("solve") && 
                    (retType!= "Boolean" || methodName.StartsWith("is") || methodName.StartsWith("has") || methodName.StartsWith("needs")) &&
                    methodName != "showEditDlg()" &&
                    !method.GetParameters().Any(p => !p.IsOptional)) {
                    try {
                        var value = method.Invoke(targetObjs, new object[method.GetParameters().Length]);
                        dgvObjectMethods.Rows.Add(methodName, retType, value?.ToString() ?? "null");
                    } catch {
                        dgvObjectMethods.Rows.Add(methodName, retType, "error");
                    }
                } else {
                    dgvObjectMethods.Rows.Add(methodName, retType, "Method");
                }

                // Print saved values
                if (methodName == "getValueNames()") {
                    getValueNamesMethod = method;
                } else if (methodName == "getValueUnits()") {
                    getValueUnitsMethod = method;
                } else if (methodName == "getValue()") {
                    getValueMethod = method;
                }
            }

            // Print object saved variables
            if (getValueNamesMethod != null) {
                string[] savedValueNames = CastObject<string>.arrayFromArrayObject(getValueNamesMethod.Invoke((object)targetObjs, new object[getValueNamesMethod.GetParameters().Length]));
                foreach (var l_name in savedValueNames) {
                    try {
                        // Create an object with the passed arguments and set the first as the value name
                        var arguments = new object[getValueMethod.GetParameters().Length];
                        arguments[0] = l_name;
                        // Get value
                        var value = getValueMethod.Invoke((object)targetObjs, arguments);

                        // Get value units
                        arguments = new object[getValueUnitsMethod.GetParameters().Length];
                        arguments[0] = l_name;
                        var units = getValueUnitsMethod.Invoke((object)targetObjs, arguments);

                        // Get value type
                        string varType = value.GetType().ToString();
                        if (varType.StartsWith("System.")) varType = varType.Substring(7);
                        if (units != null && units.ToString() !=  "None") varType += $" ({units.ToString()})";

                        dgvObjectMethods.Rows.Add($"getValue('{l_name}')", varType, value?.ToString() ?? "null");
                    } catch {
                        dgvObjectMethods.Rows.Add($"getValue('{l_name}')", "n/a", "error");
                    }
                }
            }

            // Load fields
            //var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            //foreach (var field in fields)
            //{
            //    dataGridView1.Rows.Add(field.Name, "", "Field");
            //}

            // Add manual calculations
            IFObjectSet objset = targetObjs as IFObjectSet;
            if (objset != null) {
                // Total volume
                double vlmsVolume = CastObject<IFVolume>.arrayFromArrayObject(objset.getObjects("volume")).Select(s => s.getVolume()).DefaultIfEmpty(0).Sum();
                dgvObjectMethods.Rows.Add("Total volumes volume", "Calculation", vlmsVolume);
                // Total surface area
                double surfsArea = objset.getSurfaces_Ext().Select(s => s.getArea()).DefaultIfEmpty(0).Sum();
                dgvObjectMethods.Rows.Add("Total surfaces area", "Calculation", surfsArea);
                // Total lines length
                double linesLength = objset.getLines_Ext().Select(s => s.getLineLength()).DefaultIfEmpty(0).Sum();
                dgvObjectMethods.Rows.Add("Total lines length", "Calculation", linesLength);

                // Total elements volume / area / length
                long elms3D = 0;
                long elms2D = 0;
                long elms1D = 0;
                double elmsLength = 0;
                double elmsArea = 0;
                double elmsVolume = 0;
                foreach (IFElement elm in objset.getElements_Ext())
                {
                    double l_length = elm.getLength();
                    if (l_length > 0) {
                        elms1D += 1;
                        elmsLength += l_length;
                        continue;
                    }
                    double l_area = elm.getArea();
                    if (l_area > 0)
                    {
                        elms2D += 1;
                        elmsArea += l_area;
                        continue;
                    }
                    double l_volume = elm.getVolume();
                    if (l_area > 0)
                    {
                        elms3D += 1;
                        elmsVolume += l_volume;
                        continue;
                    }
                    // It shouldn't reach this point
                }
                dgvObjectMethods.Rows.Add("Number of 1D elements", "Calculation", elms1D);
                dgvObjectMethods.Rows.Add("Total length of elements", "Calculation", elmsLength);
                dgvObjectMethods.Rows.Add("Number of 2D elements", "Calculation", elms2D);
                dgvObjectMethods.Rows.Add("Total area of elements", "Calculation", elmsArea);
                dgvObjectMethods.Rows.Add("Number of 3D elements", "Calculation", elms3D);
                dgvObjectMethods.Rows.Add("Total volume of elements", "Calculation", elmsVolume);
            }

            this.Cursor = Cursors.Default;
        }

        /// <summary>Populate treeview with all database objects</summary>
        public void PopulateTreeView()
        {
            string searchTerm = txtSearchTree.Text.Trim();

            // Clear the TreeView
            treeView.Nodes.Clear();

            // Get all volumes/surfaces/lines/points and populate the tree
            foreach (var l_geomName in new string[] { "Volume", "Surface", "Line", "Point" }) {
                var geoms = CastObject<IFGeometry>.arrayFromArrayObject(m_modeller.db().getObjects(l_geomName));

                TreeNode l_mainNode = new TreeNode($"{l_geomName}s ({geoms.Length})");
                treeView.Nodes.Add(l_mainNode);

                foreach (var l_geom in geoms) {
                    string l_name = $"{l_geomName} {l_geom.getID()}";

                    // Skip if filtered
                    if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    TreeNode l_node = new TreeNode(l_name);
                    l_node.Tag = l_geom;
                    l_mainNode.Nodes.Add(l_node);

                    // Populate surfaces for each volume
                    PopulateLOF(l_geom, l_node);
                }
            }

            // Get all attributes
            IFAttribute[] attrs = CastObject<IFAttribute>.arrayFromArrayObject(m_modeller.db().getAttributes_Ext("all"));
            TreeNode attsNode = new TreeNode($"Attributes ({attrs.Length})");
            treeView.Nodes.Add(attsNode);
            foreach (var l_attr in attrs) {
                string attrType = l_attr.getAttributeType();
                TreeNode target_cat_node = null;
                foreach (TreeNode l_cat_node in attsNode.Nodes) {
                    if (l_cat_node.Text != attrType) continue;
                    target_cat_node = l_cat_node;
                    break;
                }
                if (target_cat_node == null) { 
                    target_cat_node = new TreeNode(attrType);
                    attsNode.Nodes.Add(target_cat_node);
                }

                string l_name = l_attr.getIDAndName();
                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_attr;
                target_cat_node.Nodes.Add(l_node);
            }

            // Analyses
            var analyses = m_modeller.db().getAnalyses_Ext();
            TreeNode analysesNode = new TreeNode($"Analyses ({analyses.Length})");
            treeView.Nodes.Add(analysesNode);
            foreach (var l_analysis in analyses) {
                string l_name = l_analysis.getName();
                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_analysis;
                analysesNode.Nodes.Add(l_node);
            }

            // Loadsets
            var loadsets = CastObject<IFLoadset>.arrayFromArrayObject(m_modeller.db().getLoadsets("all"));
            TreeNode loadsetsNode = new TreeNode($"Loadsets ({loadsets.Length})");
            treeView.Nodes.Add(loadsetsNode);
            foreach (var l_loadset in loadsets) {
                string l_name = l_loadset.getName();
                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_loadset;
                loadsetsNode.Nodes.Add(l_node);
            }

            // Groups
            var groups = m_modeller.db().getGroups_Ext();
            TreeNode groupsNode = new TreeNode($"Groups ({groups.Length})");
            treeView.Nodes.Add(groupsNode);
            foreach (var l_group in groups) {
                string l_name = l_group.getName();
                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_group;
                groupsNode.Nodes.Add(l_node);
            }
        }

        /// <summary>Add geometry lower order features in the given treeview option</summary>
        /// <param name="geom">Target geometry</param>
        /// <param name="node">Target treeview node</param>
        private void PopulateLOF(IFGeometry geom, TreeNode node)
        {
            var typeCode = geom.getTypeCode();
            if (typeCode > 5) return;

            var loGeom = geom.getLOFs_Ext();
            foreach (var l_geom in loGeom)
            {
                var l_typeCode = l_geom.getTypeCode();
                if (l_typeCode > 5) continue;

                TreeNode l_node = new TreeNode($"{typeCodeToStr(l_geom)} {l_geom.getID()}");
                l_node.Tag = l_geom;
                node.Nodes.Add(l_node);

                // Populate lines for each surface
                PopulateLOF(l_geom, l_node);
            }
        }

        /// <summary>Convert object type code to string</summary>
        /// <param name="geom">Target geometry</param>
        private string typeCodeToStr(IFGeometry geom)
        {
            var typeCode = geom.getTypeCode();
            switch (typeCode)
            {
                case 1:
                    return "Point";
                case 2:
                    return "Line";
                case 3:
                    return "Combined Line";
                case 4:
                    return "Surface";
                case 5:
                    return "Volume";
            }
            return "N/A";
        }

        /// <summary>
        /// Filters the rows of the specified <see cref="DataGridView"/> based on a search term and an optional
        /// condition.
        /// </summary>
        /// <remarks>
        /// Rows are hidden if they do not match the search term or, when the "Values Only"
        /// option is enabled,  if the value in the third column is "Method". The search is case-insensitive and checks
        /// all cells  in each row for a match.
        /// </remarks>
        /// <param name="dgv">The <see cref="DataGridView"/> to filter.</param>
        /// <param name="searchTerm">
        /// The term to search for within the cells of the <paramref name="dgv"/>.  If empty, all rows are displayed
        /// unless filtered by other conditions.
        /// </param>
        private void filterDataGridView(DataGridView dgv, string searchTerm)
        {
            var valuesOnly = cbValuesOnly.Checked;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                // Hide rows that are not values only
                if (valuesOnly && row.Cells[2].Value.ToString() == "Method")
                {
                    row.Visible = false;
                    continue;
                }

                // Check if not searching
                if (searchTerm == "") {
                    row.Visible = true;
                    continue;
                }

                bool rowVisible = false;

                // Check if any cell in the row contains the search term
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null &&
                        cell.Value.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        rowVisible = true;
                        break;
                    }
                }

                // Set the row visibility based on whether it matched the search
                row.Visible = rowVisible;
            }
        }

        #region "Events"

        /// <summary>Event triggered when a node in the tree view is clicked.</summary>
        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag == null) return;
            // Populate table based on the object associated with the clicked node
            PopulateDataGridView(e.Node.Tag);
        }

        /// <summary>Event triggered when the text in the search box for tree view is changed.</summary>
        private void txtSearchTree_TextChanged(object sender, EventArgs e) {
            PopulateTreeView();
        }

        /// <summary>Event triggered when the text in the search box for methods is changed.</summary>
        private void txtSearchMethods_TextChanged(object sender, EventArgs e)
        {
            string searchTerm = txtSearchMethods.Text.Trim();
            filterDataGridView(dgvObjectMethods, searchTerm);
        }

        /// <summary>Event triggered when the "Select" button is clicked.</summary>
        private void btnHighlight_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null) return;
            IFDatabaseMember member = treeView.SelectedNode.Tag as IFDatabaseMember;
            if (member == null) return;
            m_modeller.selection().remove("all");
            m_modeller.selection().add(member);
        }

        //private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex >= 0 && e.ColumnIndex == 0)
        //    {
        //        var propertyName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
        //        var propertyInfo = targetObject.GetType().GetProperty(propertyName);

        //        if (propertyInfo != null)
        //        {
        //            var propertyValue = propertyInfo.GetValue(targetObject);
        //            // Display the propertyValue in a more detailed way, e.g., a TextBox or a RichTextBox
        //            richTextBox1.Text = $"Property: {propertyName}\nValue: {propertyValue}";
        //        }
        //    }
        //}

        /// <summary>Even triggered when the "Values Only" checkbox is checked or unchecked.</summary>
        private void cbValuesOnly_CheckedChanged(object sender, EventArgs e) {
            string searchTerm = txtSearchMethods.Text.Trim();
            filterDataGridView(dgvObjectMethods, searchTerm);
        }

        #endregion
    }
}