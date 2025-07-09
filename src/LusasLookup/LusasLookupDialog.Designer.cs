
namespace LusasLookup
{
    partial class LusasLookupDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LusasLookupDialog));
            this.dgvObjectMethods = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.treeView = new System.Windows.Forms.TreeView();
            this.btnHighlight = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lblSearchTree = new System.Windows.Forms.Label();
            this.txtSearchTree = new System.Windows.Forms.TextBox();
            this.lblSearchMethods = new System.Windows.Forms.Label();
            this.txtSearchMethods = new System.Windows.Forms.TextBox();
            this.cbValuesOnly = new System.Windows.Forms.CheckBox();
            this.txtViewObj = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.DefaultErrorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ModuleBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvObjectMethods)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvObjectMethods
            // 
            this.dgvObjectMethods.AllowUserToAddRows = false;
            this.dgvObjectMethods.AllowUserToDeleteRows = false;
            resources.ApplyResources(this.dgvObjectMethods, "dgvObjectMethods");
            this.dgvObjectMethods.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvObjectMethods.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvObjectMethods.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colName,
            this.colType,
            this.colValue});
            this.dgvObjectMethods.Name = "dgvObjectMethods";
            this.dgvObjectMethods.RowHeadersVisible = false;
            // 
            // colName
            // 
            resources.ApplyResources(this.colName, "colName");
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            // 
            // colType
            // 
            resources.ApplyResources(this.colType, "colType");
            this.colType.Name = "colType";
            this.colType.ReadOnly = true;
            // 
            // colValue
            // 
            resources.ApplyResources(this.colValue, "colValue");
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            // 
            // treeView
            // 
            resources.ApplyResources(this.treeView, "treeView");
            this.treeView.Name = "treeView";
            // 
            // btnHighlight
            // 
            resources.ApplyResources(this.btnHighlight, "btnHighlight");
            this.btnHighlight.Name = "btnHighlight";
            this.btnHighlight.UseVisualStyleBackColor = true;
            this.btnHighlight.Click += new System.EventHandler(this.btnHighlight_Click);
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lblSearchTree);
            this.splitContainer1.Panel1.Controls.Add(this.txtSearchTree);
            this.splitContainer1.Panel1.Controls.Add(this.treeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.txtViewObj);
            this.splitContainer1.Panel2.Controls.Add(this.cbValuesOnly);
            this.splitContainer1.Panel2.Controls.Add(this.lblSearchMethods);
            this.splitContainer1.Panel2.Controls.Add(this.txtSearchMethods);
            this.splitContainer1.Panel2.Controls.Add(this.btnHighlight);
            this.splitContainer1.Panel2.Controls.Add(this.dgvObjectMethods);
            // 
            // lblSearchTree
            // 
            resources.ApplyResources(this.lblSearchTree, "lblSearchTree");
            this.lblSearchTree.Name = "lblSearchTree";
            // 
            // txtSearchTree
            // 
            resources.ApplyResources(this.txtSearchTree, "txtSearchTree");
            this.txtSearchTree.Name = "txtSearchTree";
            // 
            // lblSearchMethods
            // 
            resources.ApplyResources(this.lblSearchMethods, "lblSearchMethods");
            this.lblSearchMethods.Name = "lblSearchMethods";
            // 
            // txtSearchMethods
            // 
            resources.ApplyResources(this.txtSearchMethods, "txtSearchMethods");
            this.txtSearchMethods.Name = "txtSearchMethods";
            this.txtSearchMethods.TextChanged += new System.EventHandler(this.txtSearchMethods_TextChanged);
            // 
            // cbValuesOnly
            // 
            resources.ApplyResources(this.cbValuesOnly, "cbValuesOnly");
            this.cbValuesOnly.Name = "cbValuesOnly";
            this.cbValuesOnly.UseVisualStyleBackColor = true;
            this.cbValuesOnly.CheckedChanged += new System.EventHandler(this.cbValuesOnly_CheckedChanged);
            // 
            // txtViewObj
            // 
            this.txtViewObj.BackColor = System.Drawing.SystemColors.Control;
            this.txtViewObj.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.txtViewObj, "txtViewObj");
            this.txtViewObj.Name = "txtViewObj";
            this.txtViewObj.ReadOnly = true;
            // 
            // LusasLookupDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "LusasLookupDialog";
            ((System.ComponentModel.ISupportInitialize)(this.DefaultErrorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ModuleBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvObjectMethods)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvObjectMethods;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Button btnHighlight;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.Label lblSearchTree;
        private System.Windows.Forms.TextBox txtSearchTree;
        private System.Windows.Forms.Label lblSearchMethods;
        private System.Windows.Forms.TextBox txtSearchMethods;
        private System.Windows.Forms.TextBox txtViewObj;
        private System.Windows.Forms.CheckBox cbValuesOnly;
    }
}

