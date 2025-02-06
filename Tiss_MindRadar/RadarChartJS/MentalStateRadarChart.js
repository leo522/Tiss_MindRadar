//MentalStateRadarChart.js
function renderRadarChart(canvasId, radarData) {
    if (!Array.isArray(radarData) || radarData.length === 0) {
        Swal.fire({
            icon: 'warning',
            title: '沒有檢測數據',
            text: '當前日期沒有檢測數據，請選擇其他日期！',
            confirmButtonText: '確定'
        });
        return;
    }

    console.log("Rendering Radar Chart with Data:", radarData);  // 確保數據正確

    const labels = radarData.map(item => item.CategoryName);
    const scores = radarData.map(item => item.AverageScore ? Math.round(parseFloat(item.AverageScore)) : 0); // **取整數**

    const ctx = document.getElementById(canvasId).getContext('2d');

    let radarChartInstance = new Chart(ctx, {
        type: 'radar',
        data: {
            labels: labels,
            datasets: [{
                label: '平均分數',
                data: scores,
                backgroundColor: 'rgba(255, 159, 64, 0.2)',
                borderColor: 'rgba(255, 159, 64, 1)',
                pointBackgroundColor: 'rgba(255, 159, 64, 1)',
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
                    pointLabels: { font: { size: 16 } },
                    ticks: {
                        font: { size: 14 },
                        stepSize: 1,  // **確保刻度間隔為 1**
                        callback: function (value) { return Math.round(value); } // **顯示整數**
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: { font: { size: 14 } }
                }
            }
        }
    });
}
