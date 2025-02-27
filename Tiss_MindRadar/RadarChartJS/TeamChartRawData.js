// **函式：計算最高 & 最低分**
function calculateMinMaxScores(radarData) {
    if (!Array.isArray(radarData) || radarData.length === 0) {
        console.warn("❌ 沒有有效的分數數據，無法計算最高 & 最低分");
        return null;
    }

    const scores = radarData.map(item => parseFloat(item.AverageScore)).filter(score => !isNaN(score));

    if (scores.length === 0) {
        console.warn("❌ 沒有有效的數值型分數，無法計算");
        return null;
    }

    const maxScore = Math.max(...scores);
    const minScore = Math.min(...scores);

    // 找出最高 & 最低分的類別
    const maxCategories = radarData
        .filter(item => parseFloat(item.AverageScore) === maxScore)
        .map(item => item.CategoryName);
    const minCategories = radarData
        .filter(item => parseFloat(item.AverageScore) === minScore)
        .map(item => item.CategoryName);

    console.log(`✅ 最高分: ${maxScore} (${maxCategories.join(", ")})`);
    console.log(`✅ 最低分: ${minScore} (${minCategories.join(", ")})`);

    return {
        maxScore,
        minScore,
        maxCategories,
        minCategories
    };
}

// **匯出函式，以便在前端使用**
window.calculateMinMaxScores = calculateMinMaxScores;
