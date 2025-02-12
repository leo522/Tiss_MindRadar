//心理狀態檢測向度雷達圖.js
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

    // 找出最高分 & 最低分
    const maxScore = Math.max(...scores);
    const minScore = Math.min(...scores);

    // 依分數來決定點的樣式
    const pointBackgroundColors = scores.map(score =>
        score === maxScore ? 'red' :
            score === minScore ? 'blue' : 'rgba(255, 159, 64, 1)'
    );

    const pointRadius = scores.map(score =>
        score === maxScore || score === minScore ? 8 : 5 // **最高 & 最低分點加大**
    );

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
                pointBackgroundColor: pointBackgroundColors, // **變色**
                pointBorderColor: '#fff',
                pointRadius: pointRadius, // **最大 & 最小點加大**
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
                },
                datalabels: {
                    color: 'black', // 文字顏色
                    anchor: 'end', // 文字對齊方式
                    align: 'top', // 文字位置
                    font: {
                        size: 14,
                        weight: 'bold'
                    },
                    formatter: function (value) {
                        return value.toFixed(1); // 顯示 1 位小數
                    }
                }
            }
        },
        plugins: [ChartDataLabels] // **啟用 DataLabels 插件**
    });
}