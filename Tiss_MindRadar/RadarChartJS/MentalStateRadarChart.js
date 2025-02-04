//MentalStateRadarChart.js
function renderRadarChart(canvasId, radarData) {
    if (!Array.isArray(radarData)) {
        console.error("錯誤: radarData 不是陣列！", radarData);
        return;
    }

    const labels = radarData.map(item => item.CategoryName);
    const scores = radarData.map(item => item.AverageScore);

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

    const selectedColor = 'orange';
    const chartElement = document.getElementById(canvasId);

    if (!chartElement) {
        console.error("錯誤: 找不到 #" + canvasId);
        return;
    }

    const ctx = chartElement.getContext('2d');

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
                    pointLabels: {
                        font: { size: 20 }
                    },
                    ticks: {
                        font: { size: 18 }
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        font: { size: 18 }
                    }
                }
            }
        }
    });
}