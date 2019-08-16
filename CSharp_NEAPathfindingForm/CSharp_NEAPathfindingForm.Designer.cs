namespace CSharp_NEAPathfinding
{
    partial class Form1
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
            this.pnlGrid = new System.Windows.Forms.Panel();
            this.menuBar = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuBarNew = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuBarImportImage = new System.Windows.Forms.ToolStripMenuItem();
            this.menuBarExportBmp = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuBarExit = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuBarClearMap = new System.Windows.Forms.ToolStripMenuItem();
            this.tabScreen = new System.Windows.Forms.TabPage();
            this.tabCtrl = new System.Windows.Forms.TabControl();
            this.tabTiles = new System.Windows.Forms.TabPage();
            this.tabOutput = new System.Windows.Forms.TabPage();
            this.menuBar.SuspendLayout();
            this.tabCtrl.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlGrid
            // 
            this.pnlGrid.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.pnlGrid.Location = new System.Drawing.Point(12, 35);
            this.pnlGrid.Name = "pnlGrid";
            this.pnlGrid.Size = new System.Drawing.Size(410, 366);
            this.pnlGrid.TabIndex = 0;
            // 
            // menuBar
            // 
            this.menuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuBar.Location = new System.Drawing.Point(0, 0);
            this.menuBar.Name = "menuBar";
            this.menuBar.Size = new System.Drawing.Size(648, 24);
            this.menuBar.TabIndex = 1;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuBarNew,
            this.toolStripMenuItem2,
            this.menuBarImportImage,
            this.menuBarExportBmp,
            this.toolStripMenuItem1,
            this.menuBarExit});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // menuBarNew
            // 
            this.menuBarNew.Name = "menuBarNew";
            this.menuBarNew.Size = new System.Drawing.Size(152, 22);
            this.menuBarNew.Text = "New";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(149, 6);
            // 
            // menuBarImportImage
            // 
            this.menuBarImportImage.Name = "menuBarImportImage";
            this.menuBarImportImage.Size = new System.Drawing.Size(152, 22);
            this.menuBarImportImage.Text = "Import image";
            // 
            // menuBarExportBmp
            // 
            this.menuBarExportBmp.Name = "menuBarExportBmp";
            this.menuBarExportBmp.Size = new System.Drawing.Size(152, 22);
            this.menuBarExportBmp.Text = "Export to bmp";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
            // 
            // menuBarExit
            // 
            this.menuBarExit.Name = "menuBarExit";
            this.menuBarExit.Size = new System.Drawing.Size(152, 22);
            this.menuBarExit.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuBarClearMap});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // menuBarClearMap
            // 
            this.menuBarClearMap.Name = "menuBarClearMap";
            this.menuBarClearMap.Size = new System.Drawing.Size(128, 22);
            this.menuBarClearMap.Text = "Clear Map";
            // 
            // tabScreen
            // 
            this.tabScreen.Location = new System.Drawing.Point(4, 22);
            this.tabScreen.Name = "tabScreen";
            this.tabScreen.Padding = new System.Windows.Forms.Padding(3);
            this.tabScreen.Size = new System.Drawing.Size(200, 340);
            this.tabScreen.TabIndex = 0;
            this.tabScreen.Text = "Screen";
            this.tabScreen.UseVisualStyleBackColor = true;
            // 
            // tabCtrl
            // 
            this.tabCtrl.Controls.Add(this.tabScreen);
            this.tabCtrl.Controls.Add(this.tabTiles);
            this.tabCtrl.Controls.Add(this.tabOutput);
            this.tabCtrl.Location = new System.Drawing.Point(428, 35);
            this.tabCtrl.Name = "tabCtrl";
            this.tabCtrl.SelectedIndex = 0;
            this.tabCtrl.Size = new System.Drawing.Size(208, 366);
            this.tabCtrl.TabIndex = 2;
            // 
            // tabTiles
            // 
            this.tabTiles.Location = new System.Drawing.Point(4, 22);
            this.tabTiles.Name = "tabTiles";
            this.tabTiles.Padding = new System.Windows.Forms.Padding(3);
            this.tabTiles.Size = new System.Drawing.Size(200, 340);
            this.tabTiles.TabIndex = 1;
            this.tabTiles.Text = "Tiles";
            this.tabTiles.UseVisualStyleBackColor = true;
            // 
            // tabOutput
            // 
            this.tabOutput.Location = new System.Drawing.Point(4, 22);
            this.tabOutput.Name = "tabOutput";
            this.tabOutput.Padding = new System.Windows.Forms.Padding(3);
            this.tabOutput.Size = new System.Drawing.Size(200, 340);
            this.tabOutput.TabIndex = 2;
            this.tabOutput.Text = "Output";
            this.tabOutput.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(648, 413);
            this.Controls.Add(this.tabCtrl);
            this.Controls.Add(this.pnlGrid);
            this.Controls.Add(this.menuBar);
            this.MainMenuStrip = this.menuBar;
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuBar.ResumeLayout(false);
            this.menuBar.PerformLayout();
            this.tabCtrl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlGrid;
        private System.Windows.Forms.MenuStrip menuBar;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuBarExit;
        private System.Windows.Forms.TabPage tabScreen;
        private System.Windows.Forms.TabControl tabCtrl;
        private System.Windows.Forms.TabPage tabTiles;
        private System.Windows.Forms.ToolStripMenuItem menuBarNew;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuBarClearMap;
        private System.Windows.Forms.ToolStripMenuItem menuBarExportBmp;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem menuBarImportImage;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.TabPage tabOutput;
    }
}

