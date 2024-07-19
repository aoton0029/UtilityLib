using System.Data;
using System.Windows.Forms;
using UtilityLib;

namespace Sample
{
    public partial class Form1 : Form
    {
        DataTable sampleData;

        public Form1()
        {
            InitializeComponent();
        }

        public void changeUc(UserControl uc)
        {

        }

        private void rdbSerail_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            DataTable leftTable = new DataTable();
            leftTable.Columns.Add("�u���b�N�R�[�h", typeof(int));
            leftTable.Columns.Add("�u���b�N��", typeof(string));
            Random random = new Random();

            for (int i = 1; i <= 30; i++)
            {
                int rand = random.Next(1, 5);
                leftTable.Rows.Add(rand, "�u���b�N_" + rand);
            }

            // Right�e�[�u���̃T���v���f�[�^
            DataTable rightTable = new DataTable();
            rightTable.Columns.Add("�u���b�N�R�[�h", typeof(int));
            rightTable.Columns.Add("�p�[�c�i���o�[", typeof(string));

            int x = 1;
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 1; j <= 3; j++)
                {
                    rightTable.Rows.Add(i, "PN_" + x++);
                }
            }

            DataTable resultTable = LeftJoin(leftTable, rightTable, "�u���b�N�R�[�h", "�u���b�N�R�[�h");

            grid1.DataSource = leftTable;
            grid2.DataSource = rightTable;
            gridResult.DataSource = resultTable;

        }

        public static DataTable LeftJoin(DataTable leftTable, DataTable rightTable, string leftJoinColumn, string rightJoinColumn)
        {
            var resultTable = new DataTable();

            // ���e�[�u���̃J�������R�s�[
            foreach (DataColumn column in leftTable.Columns)
            {
                resultTable.Columns.Add(new DataColumn(column.ColumnName, column.DataType));
            }

            // �E�e�[�u���̃J�������R�s�[
            foreach (DataColumn column in rightTable.Columns)
            {
                // �d������J����������ꍇ�̓��l�[������i�I�v�V�����j
                if (resultTable.Columns.Contains(column.ColumnName))
                {
                    resultTable.Columns.Add(new DataColumn(column.ColumnName + "_Right", column.DataType));
                }
                else
                {
                    resultTable.Columns.Add(new DataColumn(column.ColumnName, column.DataType));
                }
            }

            // Left Join �����s
            var query = from leftRow in leftTable.AsEnumerable()
                        join rightRow in rightTable.AsEnumerable()
                        on leftRow[leftJoinColumn] equals rightRow[rightJoinColumn] into tempJoin
                        from tempRow in tempJoin.DefaultIfEmpty()
                        select new
                        {
                            LeftData = leftRow.ItemArray,
                            RightData = tempRow != null ? tempRow.ItemArray : rightTable.NewRow().ItemArray
                        };

            // ���ʂ�V�����e�[�u���ɃR�s�[
            foreach (var item in query)
            {
                var combinedRow = resultTable.NewRow();
                combinedRow.ItemArray = item.LeftData.Concat(item.RightData).ToArray();
                resultTable.Rows.Add(combinedRow);
            }

            return resultTable;
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
                    }
    }
}
