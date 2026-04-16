using System;
using Microsoft.Data.SqlClient;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DashboardApp
{
    public partial class MainWindow : Window
    {
        string connectionString = @"Server=RYAN_LUAYON1999\SQLEXPRESS;Database=DashboardDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public MainWindow()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private async void LoadDashboard()
        {
            string studentDataJson = "[]", teacherDataJson = "[]", staffDataJson = "[]";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var studentList = await GetDistribution(conn, "SELECT ID, Name, Age, Gender, Course FROM Students");
                    studentDataJson = JsonSerializer.Serialize(studentList);

                    var teacherList = await GetDistribution(conn, "SELECT ID, Name, Age, Gender, Department FROM Teachers");
                    teacherDataJson = JsonSerializer.Serialize(teacherList);

                    var staffList = await GetDistribution(conn, "SELECT ID, Name, Age, Gender, Position FROM Staff");
                    staffDataJson = JsonSerializer.Serialize(staffList);

                    // Pass the actual counts directly to the HTML generator
                    string html = GetModernHtml(studentDataJson, teacherDataJson, staffDataJson,
                                               studentList.Count, teacherList.Count, staffList.Count);

                    await browser.EnsureCoreWebView2Async(null);
                    browser.NavigateToString(html);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
            }
        }

        private async Task<List<dynamic>> GetDistribution(SqlConnection conn, string query)
        {
            var list = new List<dynamic>();
            using (var cmd = new SqlCommand(query, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var obj = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
                    for (int i = 0; i < reader.FieldCount; i++)
                        obj.Add(reader.GetName(i), reader.GetValue(i));
                    list.Add(obj);
                }
            }
            return list;
        }

        private string GetModernHtml(string sData, string tData, string stData, int sCount, int tCount, int stCount)
        {
            return $@"
<html>
<head>
    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{ --bg: #0b0f19; --card: #161b22; --accent: #58a6ff; --text: #c9d1d9; --border: #30363d; }}
        body {{ background: var(--bg); color: var(--text); font-family: 'Inter', sans-serif; padding: 30px; margin: 0; }}
        .container {{ max-width: 1000px; margin: auto; display: flex; flex-direction: column; gap: 30px; }}
        .card {{ background: var(--card); padding: 25px; border-radius: 12px; border: 1px solid var(--border); box-shadow: 0 8px 24px rgba(0,0,0,0.5); }}
        h2 {{ margin: 0; color: var(--accent); font-size: 1.4rem; }}
        .subtitle {{ color: #8b949e; margin-bottom: 25px; font-size: 0.9rem; }}
        .chart-box {{ position: relative; height: 320px; width: 100%; margin-bottom: 40px; }}
        .tabs {{ display: flex; gap: 10px; margin-bottom: 20px; }}
        .tab-btn {{ background: #21262d; color: var(--text); border: 1px solid var(--border); padding: 10px 20px; cursor: pointer; border-radius: 6px; font-weight: 600; }}
        .tab-btn.active {{ background: var(--accent); color: white; border-color: var(--accent); }}
        .table-wrap {{ overflow-x: auto; max-height: 450px; border-radius: 8px; border: 1px solid var(--border); }}
        table {{ width: 100%; border-collapse: collapse; text-align: left; background: #0d1117; }}
        th {{ padding: 14px; background: #161b22; color: var(--accent); position: sticky; top: 0; border-bottom: 2px solid var(--border); }}
        td {{ padding: 12px; border-bottom: 1px solid var(--border); color: #8b949e; }}
        tr:hover {{ background: rgba(88, 166, 255, 0.05); }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='card'>
            <h2>📊 Analytics Dashboard</h2>
            <p class='subtitle'>Visual representation of Age, Gender, and Roles</p>
            <div class='chart-box'><canvas id='sChart'></canvas></div>
            <div class='chart-box'><canvas id='tChart'></canvas></div>
            <div class='chart-box'><canvas id='stChart'></canvas></div>
        </div>
        <div class='card'>
            <h2>📄 Database Records</h2>
            <div class='tabs'>
                <button class='tab-btn active' onclick='switchTable(""students"")'>Students ({sCount})</button>
                <button class='tab-btn' onclick='switchTable(""teachers"")'>Teachers ({tCount})</button>
                <button class='tab-btn' onclick='switchTable(""staff"")'>Staff ({stCount})</button>
            </div>
            <div class='table-wrap'>
                <table>
                    <thead><tr id='tableHead'></tr></thead>
                    <tbody id='tableBody'></tbody>
                </table>
            </div>
        </div>
    </div>
    <script>
        const allData = {{ students: {sData}, teachers: {tData}, staff: {stData} }};

        function renderTable(type) {{
            const list = allData[type];
            const head = document.getElementById('tableHead');
            const body = document.getElementById('tableBody');
            head.innerHTML = ''; body.innerHTML = '';
            if (list.length === 0) return;
            Object.keys(list[0]).forEach(key => {{ head.innerHTML += `<th>${{key}}</th>`; }});
            list.forEach(item => {{
                let row = '<tr>';
                Object.values(item).forEach(val => {{ row += `<td>${{val}}</td>`; }});
                row += '</tr>';
                body.innerHTML += row;
            }});
        }}

        function switchTable(type) {{
            document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
            event.target.classList.add('active');
            renderTable(type);
        }}

        function processData(data, key) {{
            const labels = [...new Set(data.map(i => i[key]))];
            const males = labels.map(l => data.filter(i => i[key] === l && i.Gender === 'Male').length);
            const females = labels.map(l => data.filter(i => i[key] === l && i.Gender === 'Female').length);
            return {{ labels, males, females }};
        }}

        function initChart(id, rawData, mainKey, title) {{
            const p = processData(rawData, mainKey);
            new Chart(document.getElementById(id), {{
                type: 'bar',
                data: {{
                    labels: p.labels,
                    datasets: [
                        {{ label: 'Male', data: p.males, backgroundColor: '#38bdf8', borderRadius: 4 }},
                        {{ label: 'Female', data: p.females, backgroundColor: '#fb7185', borderRadius: 4 }}
                    ]
                }},
                options: {{
                    indexAxis: 'y',
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {{ 
                        legend: {{ position: 'top', labels: {{ color: '#c9d1d9' }} }},
                        title: {{ display: true, text: title, color: '#fff', font: {{ size: 16 }} }},
                        tooltip: {{
                            callbacks: {{
                                afterLabel: function(context) {{
                                    const category = context.label;
                                    const gender = context.dataset.label;
                                    const matches = rawData.filter(i => i[mainKey] === category && i.Gender === gender);
                                    // Use double curly braces for C# interpolation escape
                                    const names = matches.map(i => `${{i.ID}}: ${{i.Name}}`).join(', ');
                                    return names ? 'Members: ' + names : '';
                                }}
                            }}
                        }}
                    }},
                    scales: {{
                        x: {{ stacked: true, grid: {{ color: '#30363d' }}, ticks: {{ color: '#8b949e' }} }},
                        y: {{ stacked: true, grid: {{ display: false }}, ticks: {{ color: '#c9d1d9' }} }}
                    }}
                }}
            }});
        }}

        initChart('sChart', allData.students, 'Course', 'Student Course Distribution');
        initChart('tChart', allData.teachers, 'Department', 'Teacher Department Distribution');
        initChart('stChart', allData.staff, 'Position', 'Staff Position Distribution');
        renderTable('students');
    </script>
</body>
</html>";
        }
    }
}