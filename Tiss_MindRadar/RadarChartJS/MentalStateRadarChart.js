//MentalStateRadarChart.js 計算各日期區間檢測結果&高低分
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

    const ctx = document.getElementById(canvasId).getContext('2d');

    // **改進配色方案，確保不同日期顏色明顯**
    const colorPalette = [
        "rgba(255, 99, 132, 0.6)",   // 粉紅
        "rgba(54, 162, 235, 0.6)",   // 藍色
        "rgba(255, 206, 86, 0.6)",   // 黃色
        "rgba(75, 192, 192, 0.6)",   // 綠松色
        "rgba(153, 102, 255, 0.6)",  // 紫色
        "rgba(255, 159, 64, 0.6)"    // 橘色
    ];

    const groupedData = {};
    radarData.forEach(item => {
        if (!groupedData[item.SurveyDate]) {
            groupedData[item.SurveyDate] = [];
        }
        groupedData[item.SurveyDate].push({
            category: item.CategoryName,
            score: Math.round(parseFloat(item.AverageScore))
        });
    });

    const datasets = Object.keys(groupedData).map((date, index) => {
        const categoryData = groupedData[date];

        // **計算每個 `surveyDate` 的最高分 & 最低分**
        const scores = categoryData.map(item => item.score);
        const maxScore = Math.max(...scores);
        const minScore = Math.min(...scores);

        return {
            label: `日期: ${date}`,
            data: scores,
            backgroundColor: colorPalette[index % colorPalette.length],  // **改進顏色選擇**
            borderColor: colorPalette[index % colorPalette.length].replace("0.6", "1"),  // **加深邊框顏色**
            pointBackgroundColor: categoryData.map(item =>
                item.score === maxScore ? 'red' :
                    item.score === minScore ? 'blue' :
                        colorPalette[index % colorPalette.length].replace("0.6", "1")
            ),
            pointBorderColor: "#fff",
            pointRadius: categoryData.map(item =>
                item.score === maxScore || item.score === minScore ? 8 : 5
            ),
            borderWidth: 2
        };
    });

    const labels = radarData.map(item => item.CategoryName).filter((v, i, a) => a.indexOf(v) === i);

    let radarChartInstance = new Chart(ctx, {
        type: "radar",
        data: {
            labels: labels,
            datasets: datasets
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
                        stepSize: 1,
                        callback: function (value) {
                            return Math.round(value);
                        }
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: "top",
                    labels: {
                        font: { size: 14 },
                        boxWidth: 20, // **讓圖例的顏色框更清楚**
                    }
                },
                datalabels: {
                    color: "black",
                    anchor: "end",   // **調整錨點，使數字靠外顯示**
                    align: "start",  // **調整對齊方式，防止遮住標籤**
                    offset: -10,       // **讓數值遠離數據點**
                    align: "top",
                    font: {
                        size: 14,
                        weight: "bold"
                    },
                    formatter: function (value) {
                        return value.toFixed(1);
                    }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}