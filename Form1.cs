using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CheckReg
{
    public partial class Form1 : Form
    {
        private DataTable originalDataTable;
        private TextBox headerTextBox;
        private int headerTextBoxColumnIndex = -1;
        private ToolTip headerToolTip = new ToolTip();
        private StatusStrip statusStrip;
        private ToolStripStatusLabel rowCountLabel;
        private ToolStripStatusLabel filterStatusLabel;

        public Form1()
        {
            try
            {
                InitializeComponent();
                dataGridView1.CellMouseEnter += DataGridView1_CellMouseEnter;
                dataGridView1.CellMouseLeave += DataGridView1_CellMouseLeave;
                
                // Initialize status bar
                InitializeStatusBar();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Initializing application");
            }
        }
        
        private void InitializeStatusBar()
        {
            statusStrip = new StatusStrip();
            rowCountLabel = new ToolStripStatusLabel { BorderSides = ToolStripStatusLabelBorderSides.Right };
            filterStatusLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            
            statusStrip.Items.Add(rowCountLabel);
            statusStrip.Items.Add(filterStatusLabel);
            
            this.Controls.Add(statusStrip);
            
            // Set initial status
            UpdateStatus("No data loaded", "");
        }
        
        private void UpdateStatus(string rowCount, string filterInfo)
        {
            try
            {
                rowCountLabel.Text = rowCount;
                filterStatusLabel.Text = filterInfo;
            }
            catch (Exception ex)
            {
                // Silent exception handling for UI updates
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        private void btnLoadData_Click(object sender, EventArgs e)
        {
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Loading data");
            }
        }

        private void LoadDataMenu_Click(object sender, EventArgs e)
        {
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Loading data from menu");
            }
        }

        private void LoadData()
        {
            string filePath = "regdata.csv";
            
            try
            {
                if (!File.Exists(filePath))
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                        openFileDialog.Title = "Select a CSV file";
                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            filePath = openFileDialog.FileName;
                        }
                        else
                        {
                            // User cancelled, do nothing
                            return;
                        }
                    }
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The file '{filePath}' could not be found.");
                }

                DataTable dt = new DataTable();
                
                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        bool isFirstLine = true;
                        int lineNumber = 0;
                        
                        while (!reader.EndOfStream)
                        {
                            lineNumber++;
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            
                            try
                            {
                                var values = line.Split(',');

                                if (isFirstLine)
                                {
                                    // First column as integer, rest as string
                                    dt.Columns.Add(values[0].Trim(), typeof(int));
                                    for (int i = 1; i < values.Length; i++)
                                        dt.Columns.Add(values[i].Trim(), typeof(string));
                                    isFirstLine = false;
                                }
                                else
                                {
                                    // Validate first column is integer
                                    if (!int.TryParse(values[0], out int intVal))
                                    {
                                        throw new FormatException($"Line {lineNumber}: The value '{values[0]}' in the first column is not an integer.");
                                    }
                                    
                                    // Check if we have enough columns
                                    if (values.Length < dt.Columns.Count)
                                    {
                                        throw new FormatException($"Line {lineNumber}: Not enough columns. Expected {dt.Columns.Count}, found {values.Length}.");
                                    }
                                    
                                    // Add row, converting first column to int
                                    object[] row = new object[values.Length];
                                    row[0] = intVal;
                                    for (int i = 1; i < values.Length; i++)
                                        row[i] = i < values.Length ? values[i] : string.Empty;
                                    dt.Rows.Add(row);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error processing line {lineNumber}: {ex.Message}", ex);
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    throw new Exception($"Error reading file '{filePath}'. The file may be in use by another program.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing file '{filePath}'.", ex);
                }

                if (dt.Rows.Count == 0)
                {
                    throw new Exception("No data was loaded. The file may be empty or incorrectly formatted.");
                }

                originalDataTable = dt.Copy();
                dataGridView1.DataSource = dt;
                
                try
                {
                    ResizeColumnsToContent();
                    dataGridView1.Resize -= DataGridView1_Resize;
                    dataGridView1.Resize += DataGridView1_Resize;
                }
                catch (Exception ex)
                {
                    // Non-critical error, just log it
                    System.Diagnostics.Debug.WriteLine($"Error resizing columns: {ex.Message}");
                }
                
                // Update status bar with row count
                UpdateStatus($"Rows: {dt.Rows.Count}", $"File: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading data from '{filePath}'.", ex);
            }
        }

        private void DataGridView1_Resize(object sender, EventArgs e)
        {
            try
            {
                ResizeColumnsToContent();
            }
            catch (Exception ex)
            {
                // Don't show message box for UI events
                System.Diagnostics.Debug.WriteLine($"Error during resize: {ex.Message}");
            }
        }

        private void ResizeColumnsToContent()
        {
            if (dataGridView1.DataSource == null) return;
            
            using (Graphics g = dataGridView1.CreateGraphics())
            {
                int lastCol = dataGridView1.ColumnCount - 1;
                for (int col = 0; col < dataGridView1.ColumnCount; col++)
                {
                    if (col == lastCol) continue;
                    int maxWidth = 0;
                    // Check header width
                    string header = dataGridView1.Columns[col].HeaderText;
                    int headerWidth = (int)g.MeasureString(header, dataGridView1.Font).Width + 20;
                    maxWidth = headerWidth;
                    // Check each cell in the column
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var value = row.Cells[col].FormattedValue?.ToString() ?? "";
                        int cellWidth = (int)g.MeasureString(value, dataGridView1.Font).Width + 20;
                        if (cellWidth > maxWidth)
                            maxWidth = cellWidth;
                    }
                    dataGridView1.Columns[col].Width = maxWidth;
                }
                // Let the last column fill the rest
                if (lastCol >= 0)
                    dataGridView1.Columns[lastCol].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowHeaderSearchBox(e.ColumnIndex);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    // Sort as normal
                    var col = dataGridView1.Columns[e.ColumnIndex];
                    var dataTable = dataGridView1.DataSource as DataTable;
                    if (dataTable != null)
                    {
                        var direction = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending ? "DESC" : "ASC";
                        try
                        {
                            dataTable.DefaultView.Sort = $"{col.DataPropertyName} {direction}";
                            col.HeaderCell.SortGlyphDirection = direction == "ASC" ? SortOrder.Ascending : SortOrder.Descending;
                            
                            // Update status for sort
                            string sortInfo = $"Sorted by: {col.HeaderText} ({(direction == "ASC" ? "Ascending" : "Descending")})";
                            UpdateStatus($"Rows: {dataTable.Rows.Count}", sortInfo);
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex, $"Sorting column '{col.HeaderText}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Processing column header click");
            }
        }

        private void ShowHeaderSearchBox(int columnIndex)
        {
            try
            {
                // Remove any existing search box
                if (headerTextBox != null)
                {
                    this.Controls.Remove(headerTextBox);
                    headerTextBox.Dispose();
                    headerTextBox = null;
                }

                var rect = dataGridView1.GetCellDisplayRectangle(columnIndex, -1, true);
                headerTextBox = new TextBox
                {
                    Bounds = new Rectangle(
                        dataGridView1.Left + rect.Left,
                        dataGridView1.Top + rect.Top,
                        rect.Width,
                        rect.Height),
                    Font = dataGridView1.ColumnHeadersDefaultCellStyle.Font
                };
                headerTextBoxColumnIndex = columnIndex;
                headerTextBox.TextChanged += HeaderTextBox_TextChanged;
                headerTextBox.LostFocus += (s, e) => {
                    try
                    {
                        RemoveHeaderSearchBox();
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, "Removing search box on lost focus");
                    }
                };
                headerTextBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                    {
                        try
                        {
                            RemoveHeaderSearchBox();
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex, "Removing search box on key press");
                        }
                    }
                };
                this.Controls.Add(headerTextBox);
                headerTextBox.BringToFront();
                headerTextBox.Focus();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Showing search box");
            }
        }

        private void RemoveHeaderSearchBox()
        {
            try
            {
                var tb = headerTextBox;
                headerTextBox = null;
                headerTextBoxColumnIndex = -1;
                if (tb != null && !tb.IsDisposed)
                {
                    this.Controls.Remove(tb);
                    tb.Dispose();
                }
                // Remove all filters by resetting the DataSource
                if (originalDataTable != null)
                {
                    dataGridView1.DataSource = originalDataTable.Copy();
                    
                    // Update status - no filter active
                    UpdateStatus($"Rows: {originalDataTable.Rows.Count}", "No filter applied");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Removing search box");
            }
        }

        private void HeaderTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (originalDataTable == null || headerTextBox == null) return;

                string filterText = headerTextBox.Text.Replace("'", "''");
                string columnName = dataGridView1.Columns[headerTextBoxColumnIndex].DataPropertyName;
                string displayColumnName = dataGridView1.Columns[headerTextBoxColumnIndex].HeaderText;

                if (string.IsNullOrEmpty(filterText))
                {
                    dataGridView1.DataSource = originalDataTable.Copy();
                    UpdateStatus($"Rows: {originalDataTable.Rows.Count}", "No filter applied");
                }
                else
                {
                    string filter = $"CONVERT([{columnName}], System.String) LIKE '%{filterText}%'";
                    DataView dv = new DataView(originalDataTable);
                    try
                    {
                        dv.RowFilter = filter;
                        var filteredTable = dv.ToTable();
                        dataGridView1.DataSource = filteredTable;
                        
                        // Update status with filter info
                        UpdateStatus($"Rows: {filteredTable.Rows.Count} of {originalDataTable.Rows.Count}", 
                                     $"Filter: {displayColumnName} contains '{filterText}'");
                    }
                    catch (Exception ex)
                    {
                        // Restore original data
                        dataGridView1.DataSource = originalDataTable.Copy();
                        UpdateStatus($"Rows: {originalDataTable.Rows.Count}", "Filter error - showing all data");
                        
                        throw new Exception($"Error applying filter to column '{displayColumnName}'. " +
                                           $"The filter text '{filterText}' may contain invalid characters.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Applying filter");
            }
        }

        private void DataGridView1_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    var rect = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                    headerToolTip.Show("Left click to search; Right click to sort", dataGridView1,
                        rect.Left + rect.Width / 2, rect.Top + rect.Height / 2, 2000);
                }
            }
            catch (Exception ex)
            {
                // Don't show message for UI events
                System.Diagnostics.Debug.WriteLine($"Error showing tooltip: {ex.Message}");
            }
        }

        private void DataGridView1_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                headerToolTip.Hide(dataGridView1);
            }
            catch (Exception)
            {
                // Silently handle tooltip errors
            }
        }

        private void ExitMenu_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Closing application");
            }
        }
        
        /// <summary>
        /// Central exception handler for displaying user-friendly error messages
        /// </summary>
        private void HandleException(Exception ex, string context)
        {
            // Get the innermost exception for the most specific error message
            Exception innerException = ex;
            while (innerException.InnerException != null)
            {
                innerException = innerException.InnerException;
            }
            
            string errorMessage = $"An error occurred while {context}:\n\n{innerException.Message}";
            
            // Log the full exception details for debugging
            System.Diagnostics.Debug.WriteLine($"ERROR in {context}: {ex}");
            
            // Show user-friendly message
            MessageBox.Show(
                errorMessage,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            
            // Update status bar
            UpdateStatus(rowCountLabel.Text, $"Error: {innerException.Message}");
        }
    }
}