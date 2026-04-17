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
                            body {{ background: var(--bg); color: var(--text); font-family: 'Inter', sans-serif; padding: 20px; margin: 0; }}
        
                            .container {{ max-width: 1400px; margin: auto; display: flex; flex-direction: column; gap: 20px; }}
        
                            /* THE KEY: Makes charts side-by-side */
                            .chart-row {{ 
                                display: flex; 
                                flex-direction: row; 
                                gap: 15px; 
                                width: 100%; 
                            }}

                            .chart-card {{ 
                                flex: 1; 
                                background: var(--card); 
                                padding: 15px; 
                                border-radius: 12px; 
                                border: 1px solid var(--border); 
                                min-width: 0; 
                            }}

                            .table-card {{ 
                                background: var(--card); 
                                padding: 25px; 
                                border-radius: 12px; 
                                border: 1px solid var(--border);
                            }}

                            h2 {{ 
                                margin: 0 0 15px 0; 
                                color: var(--accent); 
                                font-size: 1.1rem; 
                            }}
                            .chart-box {{ 
                                position: relative; 
                                height: 300px; 
                                width: 100%; 
                            }}

                                                                     /* Table & Tab Styles */
                            .tabs {{ 
                                    display: flex; 
                                    gap: 10px;
                                    margin-bottom: 20px; 
                            }}
                            .tab-btn {{ 
                                    background: #21262d; 
                                    color: var(--text); 
                                    border: 1px solid var(--border); 
                                    padding: 10px 20px; 
                                    cursor: pointer; 
                                    border-radius: 6px; 
                                    font-weight: 600; 
                            }}
                            .tab-btn.active {{
                                    background: var(--accent); 
                                    color: white; 
                                    border-color: var(--accent); 
                            }}
                            .table-wrap {{
                                    overflow-x: auto; 
                                    max-height: 450px; 
                                    border-radius: 8px; 
                                    border: 1px solid var(--border); 
                            }}
                            table {{ 
                                    width: 100%; 
                                    border-collapse: collapse; 
                                    text-align: left; 
                                    background: #0d1117; 
                            }}
                            th {{ 
                                    padding: 14px; 
                                    background: #161b22; 
                                    color: var(--accent); 
                                    position: sticky; 
                                    top: 0; 
                                    border-bottom: 2px solid var(--border); 
                            }}
                            td {{ 
                                    padding: 12px; 
                                    border-bottom: 1px solid var(--border); 
                                    color: #8b949e; 
                            }}
                            tr:hover {{ 
                                    background: rgba(88, 166, 255, 0.05); 
                            }}
                         </style>
                    </head>
                <body>
                    <div class='container'>
                        <div class='chart-row'>
                                <div class='chart-card'>
                                    <h2>Student Distribution</h2>
                                    <div class='chart-box'><canvas id='sChart'></canvas></div>
                                </div>
                                <div class='chart-card'>
                                    <h2>Teacher Department</h2>
                                    <div class='chart-box'><canvas id='tChart'></canvas></div>
                                </div>
                                <div class='chart-card'>
                                    <h2>Staff Positions</h2>
                                    <div class='chart-box'><canvas id='stChart'></canvas></div>
                         </div>
                     </div>

                        <div class='table-card'>
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

                        // --- Table Logic ---
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

                                                          // --- Data Processing Helpers ---


                        function getCounts(data, key) {{
                            const counts = {{}};
                            data.forEach(i => {{ counts[i[key]] = (counts[i[key]] || 0) + 1; }});
                            return {{ labels: Object.keys(counts), values: Object.values(counts) }};
                        }}

                        function getBubbleData(data, mainKey) {{
                            const courses = [...new Set(data.map(i => i[mainKey]))];
                            return data.map(i => ({{
                                x: i.Age,
                                y: courses.indexOf(i[mainKey]),
                                r: 8,
                                name: i.Name,
                                id: i.ID
                            }}));
                        }}

                        function processData(data, key) {{
                            const labels = [...new Set(data.map(i => i[key]))];
                            const males = labels.map(l => data.filter(i => i[key] === l && i.Gender === 'Male').length);
                            const females = labels.map(l => data.filter(i => i[key] === l && i.Gender === 'Female').length);
                            return {{ labels, males, females }};
                        }}

                                                         // --- Chart Initialization ---


                        function initCharts(allData) {{
                    // 1. STUDENT BUBBLE CHART (Flipped)
                    const sCourses = [...new Set(allData.students.map(i => i.Course))];
                    new Chart(document.getElementById('sChart'), {{
                        type: 'bubble',
                        data: {{
                            datasets: [{{
                                label: 'Students',
                                data: getBubbleData(allData.students, 'Course'),
                                backgroundColor: '#58a6ff88',
                                borderColor: '#58a6ff'
                            }}]
                        }},
                        options: {{
                            indexAxis: 'y', // This makes the bubbles scale primarily along the horizontal
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {{
                                title: {{ display: true, text: 'Student Age & Course Map', color: '#fff' }},
                                tooltip: {{
                                    callbacks: {{
                                        label: (ctx) => `ID: ${{ctx.raw.id}} | ${{ctx.raw.name}} (Age: ${{ctx.raw.x}})`
                                    }}
                                }}
                            }},
                            scales: {{
                                x: {{ 
                                    title: {{ display: true, text: 'Age', color: '#8b949e' }}, 
                                    ticks: {{ color: '#8b949e' }},
                                    grid: {{ color: '#30363d' }}
                                }},
                                y: {{ 
                                    ticks: {{ 
                                        color: '#c9d1d9', 
                                        callback: (val) => sCourses[val] 
                                    }},
                                    grid: {{ display: false }}
                                }}
                            }}
                        }}
                    }});

                                                      // 2. TEACHER BAR CHART


                    const tP = processData(allData.teachers, 'Department');
                    new Chart(document.getElementById('tChart'), {{
                        type: 'bar',
                        data: {{
                            labels: tP.labels,
                            datasets: [
                                {{ label: 'Male', data: tP.males, backgroundColor: '#38bdf8', borderRadius: 4 }},
                                {{ label: 'Female', data: tP.females, backgroundColor: '#fb7185', borderRadius: 4 }}
                            ]
                        }},
                        options: {{
                            indexAxis: 'y', // Explicitly setting horizontal bars
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {{ 
                                title: {{ display: true, text: 'Teacher Gender by Department', color: '#fff' }},
                                legend: {{ labels: {{ color: '#c9d1d9' }} }}
                            }},
                            scales: {{
                                x: {{ stacked: true, grid: {{ color: '#30363d' }}, ticks: {{ color: '#8b949e' }} }},
                                y: {{ stacked: true, grid: {{ display: false }}, ticks: {{ color: '#c9d1d9' }} }}
                            }}
                        }}
                    }});

                                                    // 3. STAFF PIE CHART (Adjusted for Horizontal Layout)


                    const stP = getCounts(allData.staff, 'Position');
                    new Chart(document.getElementById('stChart'), {{
                        type: 'pie',
                        data: {{
                            labels: stP.labels,
                            datasets: [{{
                                data: stP.values,
                                backgroundColor: ['#58a6ff', '#3fb950', '#d29922', '#f85149', '#8957e5'],
                                borderWidth: 0
                            }}]
                        }},
                        options: {{
                            responsive: true,
                            maintainAspectRatio: false,
                            plugins: {{
                                title: {{ display: true, text: 'Staff Roles Distribution', color: '#fff' }},
                                legend: {{ 
                                    position: 'bottom', // Moving legend to bottom makes it feel more ""wide"" than ""tall""
                                    labels: {{ color: '#c9d1d9', padding: 20 }} 
                                }}
                            }}
                        }}
                    }});
                }}

                            initCharts(allData);
                            renderTable('students');
                        </script>
                    </body>
                </html>"
             ;
           }
        }
}