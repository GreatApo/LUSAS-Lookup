//*******************************************************************
// LusasLookupDialog.cs
// Author: Apostolos Grammatopoulos
//
// LusasLookupDialog class implementation file
//*******************************************************************

using Lusas.Common.Attributes;
using Lusas.Common.Extensions;
using Lusas.LPI;
using Lusas.Module;
using Lusas.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LusasLookup {
    /// <summary>Module dialog providing UI to access LusasLookupModule functionality</summary>
    public partial class LusasLookupDialog : LusasModuleDialog {
        private LusasLookupModule m_module; // Reference to the module 
        private IFModeller m_modeller; // Reference to Modeller

        // Add a private BindingSource for the grid at class level
        private BindingSource _gridBindingSource = new BindingSource();

        /// <summary>Constructs an instance of the dialog</summary>
        /// <param name="lusasModule"></param>
        public LusasLookupDialog(LusasLookupModule lusasModule) : base(lusasModule) {
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
            if (noOfSelected == 0) {
                // Nothing selected, target Visible or DB
                // Get visible
                targetObject = m_modeller.getVisibleSet();
                // If all are visible, then pass database
                if (m_modeller.db().count("all") == ((IFObjectSet)targetObject).count("all")) targetObject = m_modeller.db();

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
        private void PopulateDataGridView(object targetObjs) {
            txtViewObj.Text = "Loading...";
            this.Cursor = Cursors.WaitCursor;

            // Change Select / Edit button text
            btnHighlightChangeText(targetObjs);

            dgvObjectMethods.SuspendLayout();
            try {
                var previousAutoSize = dgvObjectMethods.AutoSizeColumnsMode;
                var previousRowHeadersVisible = dgvObjectMethods.RowHeadersVisible;

                dgvObjectMethods.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvObjectMethods.RowHeadersVisible = false;
                dgvObjectMethods.ReadOnly = true;
                dgvObjectMethods.AllowUserToAddRows = false;
                dgvObjectMethods.AllowUserToResizeRows = false;
                dgvObjectMethods.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                // Clear existing binding
                dgvObjectMethods.DataSource = null;
                dgvObjectMethods.Rows.Clear();
                dgvObjectMethods.Columns.Clear();

                var table = new System.Data.DataTable();
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Type", typeof(string));
                table.Columns.Add("Value", typeof(string));

                // Reflection with caching
                Type type = ComTypeHelper.GetCOMObjectType(targetObjs);
                var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                // Cache members
                PropertyInfo[] properties = type.GetProperties(flags);
                MethodInfo[] methods = type.GetMethods(flags);

                // Object title
                var getNameMethod = methods.FirstOrDefault(m => m.Name == "getName" || m.Name == "getTitle");
                if (getNameMethod != null) {
                    object objName = getNameMethod.GetValue(targetObjs);
                    txtViewObj.Text = $"{objName ?? "Unnamed"} ({type.Name})";
                } else {
                    txtViewObj.Text = $"Unnamed object ({type.Name})";
                }

                // Load properties (avoid COM calls that throw; swallow individually)
                foreach (var property in properties) {
                    string name = property.Name;
                    string ptype = property.PropertyType.Name;
                    string valStr;
                    try {
                        var value = property.GetValue(targetObjs);
                        valStr = value?.ToString() ?? "Null";
                    } catch {
                        valStr = "error";
                    }
                    table.Rows.Add(name, ptype, valStr);
                }

                // Load methods (that are possible to load)
                string[] typesToLoad = { "Int32", "UInt32", "Int64", "UInt64", "Double", "String", "Boolean", "SByte" };
                foreach (var method in methods) {
                    var methodName = method.Name + "()";
                    var retType = method.ReturnType.Name;

                    bool isSafeBoolQuery =
                        retType == "Boolean" &&
                        (method.Name.StartsWith("is") || method.Name.StartsWith("has") || method.Name.StartsWith("needs") || method.Name.StartsWith("can"));

                    bool tryInvoke =
                        typesToLoad.Contains(retType) &&
                        !methodName.StartsWith("delete") &&
                        !methodName.StartsWith("solve") &&
                        (retType != "Boolean" || isSafeBoolQuery) &&
                        methodName != "showEditDlg()" &&
                        !method.GetParameters().Any(p => !p.IsOptional);

                    if (tryInvoke) {
                        try {
                            var value = method.GetValue(targetObjs);
                            if (methodName == "getModificationTime()") {
                                var date = new DateTime(1970, 1, 1).AddSeconds(Convert.ToInt32(value));
                                value = $"{value} ({date})";
                            }
                            table.Rows.Add(methodName, retType, value?.ToString() ?? "Null");
                            continue;
                        } catch {
                            table.Rows.Add(methodName, retType, "error");
                            continue;
                        }
                    }

                    // Default: just list method signature as "Method"
                    table.Rows.Add(methodName, retType, "Method");
                }

                // Print object saved values
                var getValueNamesMethod = methods.FirstOrDefault(m => m.Name == "getValueNames");
                if (getValueNamesMethod != null) {
                    // Get available methods
                    var getValueUnitsMethod = methods.FirstOrDefault(m => m.Name == "getValueUnits");
                    var getValueMethod = methods.FirstOrDefault(m => m.Name == "getValue");

                    // Read value names
                    string[] savedValueNames = CastObject<string>.arrayFromArrayObject(getValueNamesMethod.GetValue(targetObjs));

                    foreach (var l_name in savedValueNames) {
                        try {
                            var value = getValueMethod.GetValue(targetObjs, l_name);
                            var units = getValueUnitsMethod.GetValue(targetObjs, l_name);

                            // Handle all as arrays
                            object[] values;
                            string varType = value.GetTypeAsString();
                            if (varType == "Object[]") {
                                values = (object[])value;
                            } else {
                                values = new object[] { value };
                            }

                            // Loop saved data
                            for (int i = 0; i < values.Length; i++) {
                                var l_value = values[i];
                                string l_varType = values.Length == 1 ? varType : l_value.GetTypeAsString();

                                // Add units in type
                                if (units != null && units.ToString() != "None") l_varType += $" ({units})";

                                // Load LUSAS COM object name
                                if (l_varType.StartsWith("Lusas.LPI.")) {
                                    l_varType = l_varType.Substring(10);
                                    var t_type = ComTypeHelper.GetCOMObjectType(l_value);
                                    var t_methods = t_type.GetMethods(flags);
                                    var t_getNameMethod = t_methods.FirstOrDefault(m => m.Name == "getName" || m.Name == "getTitle");
                                    if (t_getNameMethod != null) {
                                        var name = t_getNameMethod.GetValue(l_value);
                                        if (name != null) l_value = name.ToString();
                                    }
                                }

                                string l_name_counter = values.Length > 1 ? $"[{i}]" : "";
                                table.Rows.Add($"getValue('{l_name}'){l_name_counter}", l_varType, l_value?.ToString() ?? "Null");
                            }

                        } catch {
                            table.Rows.Add($"getValue('{l_name}')", "n/a", "error");
                        }
                    }
                }

                // Manual calculations
                var objset = targetObjs as IFObjectSet;
                if (objset != null) {
                    double vlmsVolume = CastObject<IFVolume>.arrayFromArrayObject(objset.getObjects("volume"))
                        .Select(s => s.getVolume()).DefaultIfEmpty(0).Sum();
                    table.Rows.Add("Total volumes volume", "Calculation", vlmsVolume.ToString());

                    double surfsArea = objset.getSurfaces_Ext().Select(s => s.getArea()).DefaultIfEmpty(0).Sum();
                    table.Rows.Add("Total surfaces area", "Calculation", surfsArea.ToString());

                    double linesLength = objset.getLines_Ext().Select(s => s.getLineLength()).DefaultIfEmpty(0).Sum();
                    table.Rows.Add("Total lines length", "Calculation", linesLength.ToString());

                    long elms3D = 0, elms2D = 0, elms1D = 0;
                    double elmsLength = 0, elmsArea = 0, elmsVolume = 0;
                    foreach (IFElement elm in objset.getElements_Ext()) {
                        double l_length = elm.getLength();
                        if (l_length > 0) {
                            elms1D += 1;
                            elmsLength += l_length;
                            continue;
                        }
                        double l_area = elm.getArea();
                        if (l_area > 0) {
                            elms2D += 1;
                            elmsArea += l_area;
                            continue;
                        }
                        double l_volume = elm.getVolume();
                        if (l_area > 0) {
                            elms3D += 1;
                            elmsVolume += l_volume;
                            continue;
                        }
                    }
                    table.Rows.Add("Number of 1D elements", "Calculation", elms1D.ToString());
                    table.Rows.Add("Total length of elements", "Calculation", elmsLength.ToString());
                    table.Rows.Add("Number of 2D elements", "Calculation", elms2D.ToString());
                    table.Rows.Add("Total area of elements", "Calculation", elmsArea.ToString());
                    table.Rows.Add("Number of 3D elements", "Calculation", elms3D.ToString());
                    table.Rows.Add("Total volume of elements", "Calculation", elmsVolume.ToString());
                }

                // Bind via BindingSource
                _gridBindingSource.DataSource = table.DefaultView; // DataView supports Filter
                dgvObjectMethods.DataSource = _gridBindingSource;

                dgvObjectMethods.RowHeadersVisible = previousRowHeadersVisible;
                dgvObjectMethods.AutoSizeColumnsMode = previousAutoSize;
            } finally {
                dgvObjectMethods.ResumeLayout();
                this.Cursor = Cursors.Default;
            }

            // Apply filter after population
            FilterDataGridView();
        }

        /// <summary>Populate treeview with all database objects</summary>
        public void PopulateTreeView() {
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

            // Get all attributes (+utilities)
            IFAttribute[] allAttrs = CastObject<IFAttribute>.arrayFromArrayObject(m_modeller.db().getAttributes_Ext("all"));

            // Attributes
            // (from all, remove the utilities and those that have no type, e.g. IFGraph)
            IFAttribute[] attrs = allAttrs.Where(a => a.getAttributeType() != null && a.canAssign()).ToArray();
            TreeNode attsNode = new TreeNode($"Attributes ({attrs.Length})");
            treeView.Nodes.Add(attsNode);
            foreach (var l_attr in attrs) {
                // Get attribute type
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

                // Get an attribute name to show
                string l_name = l_attr.getIDAndName();
                if (l_name == null) l_name = "null";

                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_attr;
                target_cat_node.Nodes.Add(l_node);
            }

            // Utilities
            IFAttribute[] utilities = allAttrs.Except(attrs).ToArray();
            TreeNode utilNode = new TreeNode($"Utilities ({utilities.Length})");
            treeView.Nodes.Add(utilNode);
            foreach (var l_attr in utilities) {
                // Get attribute type
                string attrType = l_attr.getAttributeType();
                if (attrType == null) {
                    if (l_attr is IFGraphWizard) {
                        attrType = "Graph Wizard";
                    } else {
                        attrType = "null";
                    }
                }

                TreeNode target_cat_node = null;
                foreach (TreeNode l_cat_node in utilNode.Nodes) {
                    if (l_cat_node.Text != attrType) continue;
                    target_cat_node = l_cat_node;
                    break;
                }
                if (target_cat_node == null) {
                    target_cat_node = new TreeNode(attrType);
                    utilNode.Nodes.Add(target_cat_node);
                }

                // Get an attribute name to show
                string l_name = l_attr.getIDAndName();
                if (l_name == null) {
                    if (l_attr is IFGraphWizard l_graph) {
                        // Fix for graphs wizard, use getTitle() instead
                        l_name = l_graph.getTitle();
                    } else {
                        l_name = "null";
                    }
                }

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

            // Other objects
            // Reference Paths
            string l_objName = "Reference Path";
            var objs = CastObject<IFReferencePath>.arrayFromArrayObject(m_modeller.db().getObjects(l_objName));
            TreeNode objNode = new TreeNode($"{l_objName}s ({objs.Length})");
            treeView.Nodes.Add(objNode);
            foreach (var l_obj in objs) {
                string l_name = l_obj.getName();
                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_obj;
                objNode.Nodes.Add(l_node);
            }
            // Inspection Lines
            l_objName = "Beam/Shell Slicing";
            var objs2 = CastObject<IFBeamShellSlice>.arrayFromArrayObject(m_modeller.db().getObjects(l_objName));
            objNode = new TreeNode($"{l_objName}s ({objs2.Length})");
            treeView.Nodes.Add(objNode);
            foreach (var l_obj in objs2) {
                string l_name = l_obj.getName();
                // Skip if filtered
                if (searchTerm != "" && l_name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) < 0) continue;

                TreeNode l_node = new TreeNode(l_name);
                l_node.Tag = l_obj;
                objNode.Nodes.Add(l_node);
            }
        }

        /// <summary>Add geometry lower order features in the given treeview option</summary>
        /// <param name="geom">Target geometry</param>
        /// <param name="node">Target treeview node</param>
        private void PopulateLOF(IFGeometry geom, TreeNode node) {
            var typeCode = geom.getTypeCode();
            if (typeCode > 5) return;

            var loGeom = geom.getLOFs_Ext();
            foreach (var l_geom in loGeom) {
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
        private string typeCodeToStr(IFGeometry geom) {
            var typeCode = geom.getTypeCode();
            switch (typeCode) {
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
        private void FilterDataGridView() {
            string searchTerm = txtSearchMethods.Text.Trim();
            bool valuesOnly = cbValuesOnly.Checked;

            // Build DataView filter instead of hiding rows
            // Escape single quotes for LIKE
            string esc(string s) => s.Replace("'", "''");

            var clauses = new List<string>();

            if (valuesOnly) {
                clauses.Add("Value <> 'Method'");
            }

            if (!string.IsNullOrEmpty(searchTerm)) {
                string like = $"%{esc(searchTerm)}%";
                // OR across all columns
                clauses.Add($"(Name LIKE '{like}' OR Type LIKE '{like}' OR Value LIKE '{like}')");
            }

            string filter = string.Join(" AND ", clauses);

            // Apply to BindingSource/DataView
            var view = _gridBindingSource.DataSource as System.Data.DataView;
            if (view != null) {
                view.RowFilter = filter;
                // Move current to a visible row to avoid CurrencyManager pointing to a filtered-out row
                if (_gridBindingSource.Count > 0) {
                    _gridBindingSource.Position = 0;
                } else {
                    dgvObjectMethods.ClearSelection();
                    dgvObjectMethods.CurrentCell = null;
                }
            }
        }

        /// <summary>Change the text of the "Select"/"Edit" button.</summary>
        private void btnHighlightChangeText(object targetObjs) {

            if (targetObjs is IFDatabaseMember) {
                btnHighlight.Text = "Select";
                btnHighlight.Enabled = true;

            } else if (targetObjs is IFAttribute) {
                btnHighlight.Text = "Edit";
                btnHighlight.Enabled = true;

            } else {
                btnHighlight.Enabled = false;

            }
        }

        #region "Events"

        /// <summary>Event triggered when a node in the tree view is clicked.</summary>
        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Node.Tag == null) return;

            // Populate table based on the object associated with the clicked node
            PopulateDataGridView(e.Node.Tag);
        }

        /// <summary>Event triggered when the text in the search box for tree view is changed.</summary>
        private void txtSearchTree_TextChanged(object sender, EventArgs e) {
            PopulateTreeView();
        }

        /// <summary>Event triggered when the text in the search box for methods is changed.</summary>
        private void txtSearchMethods_TextChanged(object sender, EventArgs e) {
            FilterDataGridView();
        }

        /// <summary>Event triggered when the "Select"/"Edit" button is clicked.</summary>
        private void btnHighlight_Click(object sender, EventArgs e) {
            if (treeView.SelectedNode == null) return;

            IFDatabaseMember member = treeView.SelectedNode.Tag as IFDatabaseMember;
            if (member != null) {
                m_modeller.selection().remove("all");
                m_modeller.selection().add(member);
                return;
            }
            ;

            IFAttribute attr = treeView.SelectedNode.Tag as IFAttribute;
            if (attr != null) {
                attr.showEditDlg();
                return;
            }
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
            FilterDataGridView();
        }

        #endregion
    }
}