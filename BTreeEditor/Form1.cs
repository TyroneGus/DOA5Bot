using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BTree;

namespace BTreeEditor
{
    public partial class MainForm : Form
    {
        private TreeView treeView;
        private Button btnAddSequence, btnAddSelector, btnAddCondition, btnAddPerformMove, btnDelete;
        private Button btnSave, btnLoad;
        private Button btnAddComplexCondition, btnEditComplexCondition, btnDeleteComplexCondition;
        private PropertyGrid propertyGrid;

        public MainForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeComponent()
        {
            this.Text = "Behavior Tree Editor";
            this.Size = new Size(1300, 600);
        }

        private void InitializeControls()
        {
            treeView = new TreeView
            {
                Dock = DockStyle.Left,
                Width = 500,
                AllowDrop = true
            };
            treeView.ItemDrag += TreeView_ItemDrag;
            treeView.DragEnter += TreeView_DragEnter;
            treeView.DragDrop += TreeView_DragDrop;

            propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Right,
                Width = 500
            };
            propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;
            
            btnAddSequence = CreateButton("Add Sequence Node", AddSequenceNode);
            btnAddSelector = CreateButton("Add Selector Node", AddSelectorNode);
            btnAddCondition = CreateButton("Add Condition Node", AddConditionNode);
            btnAddPerformMove = CreateButton("Add Perform Move Node", AddPerformMoveNode);
            btnDelete = CreateButton("Delete Node", DeleteSelectedNode);
            btnSave = CreateButton("Save", SaveTree);
            btnLoad = CreateButton("Load", LoadTree);

            btnAddComplexCondition = CreateButton("Add Complex Condition", AddComplexCondition);
            btnEditComplexCondition = CreateButton("Edit Complex Condition", EditComplexCondition);
            btnDeleteComplexCondition = CreateButton("Delete Complex Condition", DeleteComplexCondition);

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            buttonPanel.Controls.AddRange(new Control[] { 
                btnAddSequence, btnAddSelector, btnAddCondition, btnAddPerformMove, btnDelete, 
                btnSave, btnLoad, btnAddComplexCondition, btnEditComplexCondition, btnDeleteComplexCondition 
            });

            this.Controls.Add(treeView);
            this.Controls.Add(propertyGrid);
            this.Controls.Add(buttonPanel);

            treeView.AfterSelect += (s, e) => 
            {
                propertyGrid.SelectedObject = e.Node.Tag;
                propertyGrid.Refresh();
            };
        }
        
        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (treeView.SelectedNode != null && treeView.SelectedNode.Tag is ConditionNode conditionNode)
            {
                treeView.SelectedNode.Text = $"Condition: {conditionNode.GetConditionSummary()}";
            }
        }

        // private string GetConditionSummary(ConditionNode conditionNode)
        // {
        //     if (conditionNode.Conditions.Count > 0)
        //     {
        //         var condition = conditionNode.Conditions[0]; // Assuming we're showing the first condition
        //         return $"{condition.Property} {condition.Operator} {condition.Value}";
        //     }
        //     return "Empty";
        // }

        private Button CreateButton(string text, EventHandler clickHandler)
        {
            Button button = new Button
            {
                Text = text,
                AutoSize = true
            };
            button.Click += clickHandler;
            return button;
        }

        private void AddSequenceNode(object sender, EventArgs e)
        {
            AddNode("SequenceNode", new SequenceNode(new List<Node>()));
        }

        private void AddSelectorNode(object sender, EventArgs e)
        {
            AddNode("SelectorNode", new SelectorNode(new List<Node>()));
        }

        private void AddConditionNode(object sender, EventArgs e)
        {
            var conditionNode = new ConditionNode();
            var newComplexCondition = new BTree.ComplexCondition();
            newComplexCondition.Conditions.Add(new BTree.Condition());
            conditionNode.AddComplexCondition(newComplexCondition);
            AddNode($"Condition: {conditionNode.GetConditionSummary()}", conditionNode);
        } 
        // private void AddConditionNode(object sender, EventArgs e)
        // {
        //     var conditionNode = new ConditionNode();
        //     var newcondition = new BTree.Condition();
        //     // conditionNode.Conditions.Add(new BTree.Condition()); // Add a default condition
        //     conditionNode.AddCondition(newcondition);
        //     // AddNode("ConditionNode", conditionNode);
        //     AddNode($"Condition: {conditionNode.GetConditionSummary()}", conditionNode);
        // }

        private void AddPerformMoveNode(object sender, EventArgs e)
        {
            AddNode("PerformMoveActionNode", new PerformMoveActionNode(""));
        }

        private void AddNode(string nodeText, Node nodeObject)
        {
            TreeNode node = new TreeNode(nodeText)
            {
                Tag = nodeObject
            };
            if (treeView.SelectedNode != null)
            {
                treeView.SelectedNode.Nodes.Add(node);
                treeView.SelectedNode.Expand();
            }
            else
            {
                treeView.Nodes.Add(node);
            }
            treeView.SelectedNode = node;
        }

        private void DeleteSelectedNode(object sender, EventArgs e)
        {
            if (treeView.SelectedNode != null)
            {
                treeView.Nodes.Remove(treeView.SelectedNode);
            }
        }

        private void TreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        private void TreeView_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode targetNode = treeView.GetNodeAt(treeView.PointToClient(new Point(e.X, e.Y)));
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

            if (targetNode != null && draggedNode != null && targetNode != draggedNode)
            {
                draggedNode.Remove();
                targetNode.Nodes.Add(draggedNode);
                targetNode.Expand();
            }
        }

        private void SaveTree(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JSON files (*.json)|*.json";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Node rootNode = CreateBTreeNode(treeView.Nodes[0]);
                    BehaviorTreeSerialized.SaveTree(rootNode, saveFileDialog.FileName);
                }
            }
        }

        private BTree.Node CreateBTreeNode(TreeNode treeViewNode)
        {
            BTree.Node node = (BTree.Node)treeViewNode.Tag;

            if (node is BTree.CompositeNode compositeNode)
            {
                compositeNode.Children = new List<BTree.Node>();
                foreach (TreeNode childNode in treeViewNode.Nodes)
                {
                    compositeNode.Children.Add(CreateBTreeNode(childNode));
                }
            }else if (node is ConditionNode customConditionNode)
            {
                var btreeConditionNode = new BTree.ConditionNode();
                foreach (var wrapper in customConditionNode.WrappedComplexConditions)
                {
                    btreeConditionNode.AddComplexCondition(wrapper.GetComplexCondition());
                }

                return btreeConditionNode;
            }
            // else if (node is ConditionNode customConditionNode)
            // {
            //     // Convert our custom ConditionNode back to BTree.ConditionNode
            //     var btreeConditionNode = new BTree.ConditionNode();
            //     foreach (var wrapper in customConditionNode.WrappedConditions)
            //     {
            //         btreeConditionNode.AddCondition(wrapper.GetCondition());
            //     }
            //     return btreeConditionNode;
            // }

            return node;
        }

        private void LoadTree(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Node rootNode = BehaviorTreeSerialized.LoadTree(openFileDialog.FileName);
                    treeView.Nodes.Clear();
                    // treeView.Nodes.Add(CreateTreeViewNode(rootNode));
                    treeView.Nodes.Add(ConvertBTreeNodeToCustomNode(rootNode)); // Convert to custom node()
                }
            }
        }
        
        private TreeNode ConvertBTreeNodeToCustomNode(BTree.Node node)
        {
            TreeNode treeNode = new TreeNode();
            if (node is BTree.SequenceNode sequenceNode)
            {
                treeNode.Text = "SequenceNode";
                treeNode.Tag = new SequenceNode(new List<BTree.Node>());
            }
            else if (node is BTree.SelectorNode selectorNode)
            {
                treeNode.Text = "SelectorNode";
                treeNode.Tag = new SelectorNode(new List<BTree.Node>());
            }
            else if (node is BTree.ConditionNode conditionNode)
            {
                var customConditionNode = new ConditionNode();
                foreach (var complexCondition in conditionNode.ComplexConditions)
                {
                    customConditionNode.AddComplexCondition(complexCondition);
                }
                treeNode.Text = $"Condition: {customConditionNode.GetConditionSummary()}";
                treeNode.Tag = customConditionNode;
            }
            // {
            //     treeNode.Text = "ConditionNode";
            //     var customConditionNode = new ConditionNode();
            //     foreach (var condition in conditionNode.Conditions)
            //     {
            //         customConditionNode.AddCondition(condition);
            //     }
            //     treeNode.Text = $"Condition: {customConditionNode.GetConditionSummary()}";
            //     treeNode.Tag = customConditionNode;
            // }
            else if (node is BTree.PerformMoveActionNode performMoveNode)
            {
                treeNode.Text = $"PerformMoveActionNode: {performMoveNode.MoveName}";
                treeNode.Tag = performMoveNode;
            }

            if (node is BTree.CompositeNode compositeNode)
            {
                foreach (var childNode in compositeNode.Children)
                {
                    treeNode.Nodes.Add(ConvertBTreeNodeToCustomNode(childNode));
                }
            }

            return treeNode;
        }
        
        private void AddComplexCondition(object sender, EventArgs e)
        {
            if (treeView.SelectedNode?.Tag is ConditionNode conditionNode)
            {
                using (var form = new ComplexConditionForm())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        conditionNode.AddComplexCondition(form.ComplexCondition);
                        UpdateConditionNodeText(treeView.SelectedNode, conditionNode);
                        propertyGrid.Refresh();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a ConditionNode first.");
            }
        }

        private void EditComplexCondition(object sender, EventArgs e)
        {
            if (treeView.SelectedNode?.Tag is ConditionNode conditionNode)
            {
                using (var form = new ComplexConditionListForm(conditionNode.WrappedComplexConditions))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        conditionNode.WrappedComplexConditions = form.UpdatedComplexConditions;
                        UpdateConditionNodeText(treeView.SelectedNode, conditionNode);
                        propertyGrid.Refresh();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a ConditionNode first.");
            }
        }

        private void DeleteComplexCondition(object sender, EventArgs e)
        {
            if (treeView.SelectedNode?.Tag is ConditionNode conditionNode)
            {
                using (var form = new ComplexConditionListForm(conditionNode.WrappedComplexConditions, true))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        conditionNode.WrappedComplexConditions = form.UpdatedComplexConditions;
                        UpdateConditionNodeText(treeView.SelectedNode, conditionNode);
                        propertyGrid.Refresh();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a ConditionNode first.");
            }
        }

        private void UpdateConditionNodeText(TreeNode treeNode, ConditionNode conditionNode)
        {
            treeNode.Text = $"Condition: {conditionNode.GetConditionSummary()}";
        }
        
        private void AddConditionToComplexCondition(ConditionNode node, int complexConditionIndex)
        {
            if (complexConditionIndex < node.WrappedComplexConditions.Count)
            {
                node.WrappedComplexConditions[complexConditionIndex].Conditions.Add(new ConditionWrapper(new BTree.Condition()));
                propertyGrid.Refresh();
            }
        }

        private void RemoveConditionFromComplexCondition(ConditionNode node, int complexConditionIndex, int conditionIndex)
        {
            if (complexConditionIndex < node.WrappedComplexConditions.Count &&
                conditionIndex < node.WrappedComplexConditions[complexConditionIndex].Conditions.Count)
            {
                node.WrappedComplexConditions[complexConditionIndex].Conditions.RemoveAt(conditionIndex);
                propertyGrid.Refresh();
            }
        } 
        
        /*private TreeNode CreateTreeViewNode(Node node)
        {
            TreeNode treeNode = new TreeNode();
            if (node is SequenceNode)
            {
                treeNode.Text = "SequenceNode";
                treeNode.Tag = node;
            }
            else if (node is SelectorNode)
            {
                treeNode.Text = "SelectorNode";
                treeNode.Tag = node;
            }
            else if (node is ConditionNode conditionNode)
            {
                treeNode.Text = $"Condition: {GetConditionSummary(conditionNode)}";
                treeNode.Tag = conditionNode;
            }
            else if (node is PerformMoveActionNode performMoveNode)
            {
                treeNode.Text = $"PerformMoveActionNode: {performMoveNode.MoveName}";
                treeNode.Tag = performMoveNode;
            }

            if (node is CompositeNode compositeNode)
            {
                foreach (var childNode in compositeNode.Children)
                {
                    treeNode.Nodes.Add(CreateTreeViewNode(childNode));
                }
            }

            return treeNode;
        }*/
        
            // New form for adding/editing a single ComplexCondition
    public class ComplexConditionForm : Form
    {
        private ComboBox cmbLogicalOperator;
        private DataGridView dgvConditions;
        private Button btnOK, btnCancel, btnAddCondition, btnRemoveCondition;

        public ComplexCondition ComplexCondition { get; private set; }

        public ComplexConditionForm(ComplexCondition existingCondition = null)
        {
            InitializeComponent();
            if (existingCondition != null)
            {
                ComplexCondition = existingCondition;
                LoadExistingCondition();
            }
            else
            {
                ComplexCondition = new ComplexCondition();
            }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(600, 400);
            this.Text = "Complex Condition Editor";

            cmbLogicalOperator = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { "AND", "OR" },
                Location = new Point(10, 10),
                Width = 100
            };
            cmbLogicalOperator.SelectedIndex = 0;

            dgvConditions = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(560, 250),
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };
            dgvConditions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Property", HeaderText = "Property" });
            dgvConditions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Operator", HeaderText = "Operator" });
            dgvConditions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value" });

            btnAddCondition = new Button
            {
                Text = "Add Condition",
                Location = new Point(10, 300),
                Width = 120
            };
            btnAddCondition.Click += (s, e) => dgvConditions.Rows.Add();

            btnRemoveCondition = new Button
            {
                Text = "Remove Condition",
                Location = new Point(140, 300),
                Width = 120
            };
            btnRemoveCondition.Click += (s, e) => 
            {
                if (dgvConditions.SelectedRows.Count > 0)
                {
                    dgvConditions.Rows.RemoveAt(dgvConditions.SelectedRows[0].Index);
                }
            };

            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(400, 300),
                Width = 80
            };
            btnOK.Click += (s, e) => SaveComplexCondition();

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(490, 300),
                Width = 80
            };

            this.Controls.AddRange(new Control[] { cmbLogicalOperator, dgvConditions, btnAddCondition, btnRemoveCondition, btnOK, btnCancel });
        }

        private void LoadExistingCondition()
        {
            cmbLogicalOperator.SelectedItem = ComplexCondition.LogicalOperator;
            foreach (var condition in ComplexCondition.Conditions)
            {
                dgvConditions.Rows.Add(condition.Property, condition.Operator, condition.Value);
            }
        }

        private void SaveComplexCondition()
        {
            ComplexCondition.LogicalOperator = cmbLogicalOperator.SelectedItem.ToString();
            ComplexCondition.Conditions.Clear();
            foreach (DataGridViewRow row in dgvConditions.Rows)
            {
                ComplexCondition.Conditions.Add(new Condition
                {
                    Property = row.Cells["Property"].Value?.ToString(),
                    Operator = row.Cells["Operator"].Value?.ToString(),
                    Value = row.Cells["Value"].Value
                });
            }
        }
    }

    // New form for editing/deleting multiple ComplexConditions
    public class ComplexConditionListForm : Form
    {
        private ListBox lstComplexConditions;
        private Button btnAdd, btnEdit, btnDelete, btnOK, btnCancel;
        public List<ComplexConditionWrapper> UpdatedComplexConditions { get; private set; }

        public ComplexConditionListForm(List<ComplexConditionWrapper> complexConditions, bool deleteMode = false)
        {
            UpdatedComplexConditions = new List<ComplexConditionWrapper>(complexConditions);
            InitializeComponent(deleteMode);
            LoadComplexConditions();
        }

        private void InitializeComponent(bool deleteMode)
        {
            this.Size = new Size(400, 300);
            this.Text = deleteMode ? "Delete Complex Conditions" : "Edit Complex Conditions";

            lstComplexConditions = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(360, 180)
            };

            btnAdd = new Button
            {
                Text = "Add",
                Location = new Point(10, 200),
                Width = 80
            };
            btnAdd.Click += (s, e) => AddComplexCondition();

            btnEdit = new Button
            {
                Text = "Edit",
                Location = new Point(100, 200),
                Width = 80
            };
            btnEdit.Click += (s, e) => EditComplexCondition();

            btnDelete = new Button
            {
                Text = "Delete",
                Location = new Point(190, 200),
                Width = 80
            };
            btnDelete.Click += (s, e) => DeleteComplexCondition();

            btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 230),
                Width = 80
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 230),
                Width = 80
            };

            this.Controls.AddRange(new Control[] { lstComplexConditions, btnAdd, btnEdit, btnDelete, btnOK, btnCancel });

            if (deleteMode)
            {
                btnAdd.Visible = false;
                btnEdit.Visible = false;
            }
        }

        private void LoadComplexConditions()
        {
            lstComplexConditions.Items.Clear();
            foreach (var complexCondition in UpdatedComplexConditions)
            {
                lstComplexConditions.Items.Add(GetComplexConditionSummary(complexCondition));
            }
        }

        private string GetComplexConditionSummary(ComplexConditionWrapper complexCondition)
        {
            return $"{complexCondition.LogicalOperator}: {string.Join(", ", complexCondition.Conditions.Select(c => $"{c.Property} {c.Operator} {c.Value}"))}";
        }

        private void AddComplexCondition()
        {
            using (var form = new ComplexConditionForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    UpdatedComplexConditions.Add(new ComplexConditionWrapper(form.ComplexCondition));
                    LoadComplexConditions();
                }
            }
        }

        private void EditComplexCondition()
        {
            if (lstComplexConditions.SelectedIndex != -1)
            {
                using (var form = new ComplexConditionForm(UpdatedComplexConditions[lstComplexConditions.SelectedIndex].GetComplexCondition()))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        UpdatedComplexConditions[lstComplexConditions.SelectedIndex] = new ComplexConditionWrapper(form.ComplexCondition);
                        LoadComplexConditions();
                    }
                }
            }
        }

        private void DeleteComplexCondition()
        {
            if (lstComplexConditions.SelectedIndex != -1)
            {
                UpdatedComplexConditions.RemoveAt(lstComplexConditions.SelectedIndex);
                LoadComplexConditions();
            }
        }
    }
    }

    public class ComplexConditionWrapper
    {
        public List<ConditionWrapper> Conditions { get; set; } = new List<ConditionWrapper>();
        [TypeConverter(typeof(LogicalOperatorConverter))]
        public string LogicalOperator { get; set; } = "AND";

        public ComplexConditionWrapper(BTree.ComplexCondition complexCondition)
        {
            LogicalOperator = complexCondition.LogicalOperator;
            Conditions = complexCondition.Conditions.Select(c => new ConditionWrapper(c)).ToList();
        }

        public BTree.ComplexCondition GetComplexCondition()
        {
            return new BTree.ComplexCondition
            {
                LogicalOperator = LogicalOperator,
                Conditions = Conditions.Select(cw => cw.GetCondition()).ToList()
            };
        }
    }

    public class LogicalOperatorConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[] { "AND", "OR" });
        }
    }

    [TypeConverter(typeof(ConditionNodeTypeConverter))]
    public class ConditionNode : BTree.ConditionNode
    {
        public List<ComplexConditionWrapper> WrappedComplexConditions { get; set; } = new List<ComplexConditionWrapper>();

        public ConditionNode() : base() { }

        public new void AddComplexCondition(BTree.ComplexCondition complexCondition)
        {
            base.ComplexConditions.Add(complexCondition);
            WrappedComplexConditions.Add(new ComplexConditionWrapper(complexCondition));
        }

        public string GetConditionSummary()
        {
            if (WrappedComplexConditions.Count > 0)
            {
                var complexCondition = WrappedComplexConditions[0];
                return string.Join(" " + complexCondition.LogicalOperator + " ", 
                    complexCondition.Conditions.Select(c => $"{c.Property} {c.Operator} {c.Value}"));
            }
            return "Empty";
        }
    }
    
    // [TypeConverter(typeof(ExpandableObjectConverter))]
    // public class ConditionNodeTypeConverter : TypeConverter
    // {
    //     public override bool GetPropertiesSupported(ITypeDescriptorContext context)
    //     {
    //         return true;
    //     }
    //
    //     public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
    //     {
    //         var properties = TypeDescriptor.GetProperties(typeof(ConditionNode), attributes);
    //         return properties;
    //     }
    // }
    
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ConditionNodeTypeConverter : ExpandableObjectConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var properties = TypeDescriptor.GetProperties(typeof(ConditionNode), attributes);
            var filteredProperties = new PropertyDescriptorCollection(null);
            filteredProperties.Add(properties["WrappedConditions"]);
            return filteredProperties;
        }
    } 
    
/*    [TypeConverter(typeof(ConditionNodeTypeConverter))]
    public class ConditionNode : BTree.ConditionNode
    {
        public List<ConditionWrapper> WrappedConditions { get; set; }

        public ConditionNode() : base()
        {
            WrappedConditions = new List<ConditionWrapper>();
        }

        public new void AddCondition(BTree.Condition condition)
        {
            base.AddCondition(condition);
            WrappedConditions.Add(new ConditionWrapper(condition));
        }
        
        public string GetConditionSummary()
        {
            string summary = string.Empty;
            foreach (var condition in WrappedConditions)
            {
                summary += $"{condition.Property} {condition.Operator} {condition.Value};";
            }

            return summary;
            // if (WrappedConditions.Count > 0)
            // {
            //     var condition = WrappedConditions[0]; // Assuming we're showing the first condition
            //     return $"{condition.Property} {condition.Operator} {condition.Value}";
            // }
            // return "Empty";
        }
    }*/
    
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ConditionWrapper
    {
        private BTree.Condition _condition;

        public ConditionWrapper()
        {
            _condition = new BTree.Condition();
        }
        public ConditionWrapper(BTree.Condition condition)
        {
            _condition = condition;
        }

        [TypeConverter(typeof(GameInfoPropertyConverter))]
        public string Property
        {
            get => _condition.Property;
            set => _condition.Property = value;
        }

        [TypeConverter(typeof(OperatorConverter))]
        public string Operator
        {
            get => _condition.Operator;
            set => _condition.Operator = value;
        }

        [TypeConverter(typeof(NumericUpDownConverter))]
        public object Value
        {
            get => _condition.Value;
            set => _condition.Value = value;
        }

        public BTree.Condition GetCondition() => _condition;
    }
    
    
    // [TypeConverter(typeof(ExpandableObjectConverter))]
    // public class Condition
    // {
    //     [TypeConverter(typeof(GameInfoPropertyConverter))]
    //     public string Property { get; set; } = "GameInfo.PX_Distance";
    //
    //     [TypeConverter(typeof(OperatorConverter))]
    //     public string Operator { get; set; } = ">";
    //
    //     [TypeConverter(typeof(NumericUpDownConverter))]
    //     public float Value { get; set; } = 0f;
    // }

    public class GameInfoPropertyConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new List<string>
            {
                "GameInfo.PX_Distance",
                "GameInfo.PX_TotalActiveFrames",
                "GameInfo.Player.Airborne",
                "GameInfo.Player.ComboCounter",
                "GameInfo.Player.CurrentCharacter",
                "GameInfo.Player.CurrentMove",
                "GameInfo.Player.CurrentMoveFrame",
                "GameInfo.Player.Direction",
                "GameInfo.Player.HighMidLowGround",
                "GameInfo.Player.MoveType",
                "GameInfo.Player.MoveTypeDetailed",
                "GameInfo.Player.Stance",
                "GameInfo.Player.StrikeType",
                "GameInfo.Player.TotalRecovery",
                "GameInfo.Player.TotalStartup",
                "GameInfo.Opponent.Airborne",
                "GameInfo.Opponent.ComboCounter",
                "GameInfo.Opponent.CurrentCharacter",
                "GameInfo.Opponent.CurrentMove",
                "GameInfo.Opponent.CurrentMoveFrame",
                "GameInfo.Opponent.Direction",
                "GameInfo.Opponent.HighMidLowGround",
                "GameInfo.Opponent.MoveType",
                "GameInfo.Opponent.MoveTypeDetailed",
                "GameInfo.Opponent.Stance",
                "GameInfo.Opponent.StrikeType",
                "GameInfo.Opponent.TotalRecovery",
                "GameInfo.Opponent.TotalStartup"
            });
        }
    }

    public class OperatorConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new List<string> { ">", "<", ">=", "<=", "!=", "==" });
        }
    }

    public class NumericUpDownConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                if (float.TryParse(stringValue, out float result))
                {
                    return result;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is float)
            {
                return value.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[] { 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f });
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false; // 允许用户输入自定义值
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}