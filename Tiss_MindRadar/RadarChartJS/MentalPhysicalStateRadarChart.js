//身心狀態檢測向度雷達圖.js
Chart.register(ChartDataLabels);

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
    const scores = radarData.map(item => Math.round(item.AverageScore || 0));

    const maxScore = Math.max(...scores);
    const minScore = Math.min(...scores);
    const maxIndexes = scores.map((s, i) => s === maxScore ? i : -1).filter(i => i !== -1);
    const minIndexes = scores.map((s, i) => s === minScore ? i : -1).filter(i => i !== -1);

    console.log("Radar Data Processed:", scores);

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
                pointBackgroundColor: scores.map((s, i) =>
                    maxIndexes.includes(i) ? 'rgba(255, 0, 0, 1)' :
                        minIndexes.includes(i) ? 'rgba(0, 0, 255, 1)' :
                            borderColors[selectedColor]),
                pointBorderColor: '#fff',
                borderWidth: 2,
                pointRadius: scores.map((s, i) => maxIndexes.includes(i) || minIndexes.includes(i) ? 7 : 5),
                pointHoverRadius: 10
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
                        stepSize: 1,
                        callback: function (value) { return Math.round(value); }
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: { font: { size: 18 } }
                },
                datalabels: {
                    color: '#000',
                    font: { size: 14, weight: 'bold' },
                    align: 'end',
                    anchor: 'end',
                    formatter: function (value) {
                        return value.toFixed(1);
                    }
                }
            }
        }
    });
}