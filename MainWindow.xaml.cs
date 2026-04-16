using System;
using Microsoft.Data.SqlClient;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;

namespace DashboardApp
{
    public partial class MainWindow : Window
    {
        // Added @ for verbatim string to handle backslashes in server name
        string connectionString = @"Server=RYAN_LUAYON1999\SQLEXPRESS;Database=DashboardDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public MainWindow()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private async void LoadDashboard()
        {
            // We will store our data distributions here
            string studentDataJson = "[]", teacherDataJson = "[]", staffDataJson = "[]";
            int studentCount = 0, teacherCount = 0, staffCount = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Fetch Students (selecting all 3 required columns)
                    var studentList = await GetDistribution(conn, "SELECT Age, Gender, Course FROM Students");
                    studentDataJson = JsonSerializer.Serialize(studentList);
                    studentCount = studentList.Count;

                    // Fetch Teachers
                    var teacherList = await GetDistribution(conn, "SELECT Age, Gender, Department FROM Teachers");
                    teacherDataJson = JsonSerializer.Serialize(teacherList);
                    teacherCount = teacherList.Count;

                    // Fetch Staff
                    var staffList = await GetDistribution(conn, "SELECT Age, Gender, Position FROM Staff");
                    staffDataJson = JsonSerializer.Serialize(staffList);
                    staffCount = staffList.Count;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
            }

            string html = GetModernHtml(studentDataJson, teacherDataJson, staffDataJson, studentCount, teacherCount, staffCount);
            await browser.EnsureCoreWebView2Async(null);
            browser.NavigateToString(html);
        }

        // Helper method to turn SQL rows into a list of objects
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
    <style>
        :root {{ --bg: #0b0f19; --card: #161b22; --accent: #58a6ff; --text: #c9d1d9; }}
        body {{ background: var(--bg); color: var(--text); font-family: 'Inter', sans-serif; padding: 30px; }}
        .container {{ display: flex; flex-direction: column; gap: 40px; max-width: 1000px; margin: auto; }}
        .card {{ 
            background: var(--card); 
            padding: 30px; 
            border-radius: 12px; 
            border: 1px solid #30363d;
            box-shadow: 0 8px 24px rgba(0,0,0,0.5);
        }}
        .card-info {{ margin-bottom: 20px; }}
        h2 {{ margin: 0; color: var(--accent); font-size: 1.5rem; }}
        p {{ color: #8b949e; margin: 5px 0 0 0; }}
        .chart-box {{ position: relative; height: 350px; width: 100%; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='card'>
            <div class='card-info'><h2>🎓 Student Analytics</h2><p>Age, Gender, and Course Distribution ({sCount} Total)</p></div>
            <div class='chart-box'><canvas id='sChart'></canvas></div>
        </div>

        <div class='card'>
            <div class='card-info'><h2>👩‍🏫 Faculty Analytics</h2><p>Age, Gender, and Department Distribution ({tCount} Total)</p></div>
            <div class='chart-box'><canvas id='tChart'></canvas></div>
        </div>

        <div class='card'>
            <div class='card-info'><h2>🧑‍💼 Staff Analytics</h2><p>Age, Gender, and Position Distribution ({stCount} Total)</p></div>
            <div class='chart-box'><canvas id='stChart'></canvas></div>
        </div>
    </div>

    <script>
        const sRaw = {sData};
        const tRaw = {tData};
        const stRaw = {stData};

        function processComplexData(data, mainKey) {{
            const labels = [...new Set(data.map(item => item[mainKey]))]; // Unique Courses/Depts/Pos
            
            // We'll create datasets for Gender specifically to show how they split across the main category
            const males = labels.map(l => data.filter(i => i[mainKey] === l && i.Gender === 'Male').length);
            const females = labels.map(l => data.filter(i => i[mainKey] === l && i.Gender === 'Female').length);
            
            return {{ labels, males, females }};
        }}

        function renderModernChart(id, title, rawData, mainKey) {{
            const processed = processComplexData(rawData, mainKey);
            
            new Chart(document.getElementById(id), {{
                type: 'bar',
                data: {{
                    labels: processed.labels,
                    datasets: [
                        {{ label: 'Male', data: processed.males, backgroundColor: '#38bdf8', borderRadius: 5 }},
                        {{ label: 'Female', data: processed.females, backgroundColor: '#fb7185', borderRadius: 5 }}
                    ]
                }},
                options: {{
                    indexAxis: 'y', // Makes it horizontal
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {{
                        legend: {{ position: 'top', labels: {{ color: '#c9d1d9' }} }},
                        tooltip: {{
                            callbacks: {{
                                afterLabel: function(context) {{
                                    // Custom logic to show Age in the tooltip
                                    const category = context.label;
                                    const gender = context.dataset.label;
                                    const items = rawData.filter(i => i[mainKey] === category && i.Gender === gender);
                                    const ages = items.map(i => i.Age);
                                    return ages.length > 0 ? 'Ages: ' + ages.join(', ') : '';
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

        renderModernChart('sChart', 'Students', sRaw, 'Course');
        renderModernChart('tChart', 'Teachers', tRaw, 'Department');
        renderModernChart('stChart', 'Staff', stRaw, 'Position');
    </script>
</body>
</html>";

        }
    }
}