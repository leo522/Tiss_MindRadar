// MentalPhysicalStateRadarChart.js
function renderRadarChart(chartId, radarData) {
    if (!radarData || radarData.length === 0) {
        console.warn("Radar chart data is empty.");
        return;
    }

    const chartColors = {
        red: 'rgba(255, 99, 132, 0.2)',
        blue: 'rgba(54, 162, 235, 0.2)',
        green: 'rgba(75, 192, 192, 0.2)',
        purple: 'rgba(153, 102, 255, 0.2)',
        orange: 'rgba(255, 159, 64, 0.2)'
    };

    const borderColors = {
        red: 'rgba(255, 99, 132, 1)',
        blue: 'rgba(54, 162, 235, 1)',
        green: 'rgba(75, 192, 192, 1)',
        purple: 'rgba(153, 102, 255, 1)',
        orange: 'rgba(255, 159, 64, 1)'
    };

    const selectedColor = 'purple';

    const labels = radarData.map(item => item.CategoryName);
    const scores = radarData.map(item => Math.round(item.AverageScore || 0)); // **強制轉整數**

    console.log("Radar Data Processed:", scores); // 確保數據正確

    const ctx = document.getElementById(chartId).getContext('2d');

    new Chart(ctx, {
        type: 'radar',
        data: {
            labels: labels,
            datasets: [{
                label: '平均分數',
                data: scores,
                backgroundColor: chartColors[selectedColor],
                borderColor: borderColors[selectedColor],
                pointBackgroundColor: borderColors[selectedColor],
                pointBorderColor: '#fff',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                r: {
                    angleLines: { display: true },
                    suggestedMin: 0,
                    suggestedMax: 5,
                    pointLabels: { font: { size: 18 } },
                    ticks: {
                        font: { size: 18 },
                        stepSize: 1,  // **確保刻度間隔為 1**
                        callback: function (value) { return Math.round(value); } // **顯示整數**
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: { font: { size: 18 } }
                }
            }
        }
    });
}
