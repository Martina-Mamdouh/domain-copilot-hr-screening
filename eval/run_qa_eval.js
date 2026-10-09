const fs = require('fs');

async function runQaEval() {
    console.log("Loading Q/A ground truth data...");
    const rawData = fs.readFileSync('eval/qa_ground_truth.json');
    const groundTruth = JSON.parse(rawData);

    console.log("Authenticating as admin...");
    const loginRes = await fetch('http://localhost:8080/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: 'admin@copilot.local', password: 'Admin@123456' })
    });
    
    if (!loginRes.ok) {
        console.error("Login failed!", await loginRes.text());
        return;
    }
    
    const { token } = await loginRes.json();
    let total = groundTruth.length;
    let successfulCalls = 0;
    let retrievalHits = 0;
    let groundedCount = 0;
    let refusalCorrectCount = 0;
    let totalAdversarial = 0;

    console.log(`Starting Q/A Evaluation Suite for ${total} pairs...\n`);

    for (let i = 0; i < total; i++) {
        const item = groundTruth[i];
        const isAdversarial = item.is_adversarial || false;
        if (isAdversarial) totalAdversarial++;

        try {
            const res = await fetch('http://localhost:8080/api/retrieval/ask', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ query: item.question })
            });

            if (!res.ok) {
                console.error(`  [!] API Error on Q${i+1}: ${res.status}`);
                continue;
            }

            const result = await res.json();
            successfulCalls++;

            const hasSources = result.sources && result.sources.length > 0;
            const isGrounded = result.isGrounded !== undefined ? result.isGrounded : hasSources;
            const isRefusal = result.answer && result.answer.toLowerCase().includes("not available");

            // Evaluate Metrics
            if (!isAdversarial && hasSources) retrievalHits++;
            if (!isAdversarial && isGrounded) groundedCount++;
            if (isAdversarial && (isRefusal || !isGrounded)) refusalCorrectCount++;

            console.log(`[${i+1}/${total}] Q: "${item.question.substring(0, 45)}..."`);
            console.log(`     -> Grounded: ${isGrounded} | Sources: ${result.sources ? result.sources.length : 0} | Adversarial: ${isAdversarial}`);

        } catch (e) {
            console.error(`  [!] Exception evaluating Q${i+1}:`, e.message);
        }
    }

    const standardTotal = total - totalAdversarial;
    const hitRate = standardTotal > 0 ? ((retrievalHits / standardTotal) * 100).toFixed(1) : 0;
    const groundednessRate = standardTotal > 0 ? ((groundedCount / standardTotal) * 100).toFixed(1) : 0;
    const refusalRate = totalAdversarial > 0 ? ((refusalCorrectCount / totalAdversarial) * 100).toFixed(1) : 100;

    console.log("\n================ FR-3 METRICS SCORECARD ================");
    console.log(`Total Q/A Pairs: ${total} (Standard: ${standardTotal}, Adversarial: ${totalAdversarial})`);
    console.log(`Successful Calls: ${successfulCalls}/${total}`);
    console.log(`Retrieval Hit-Rate: ${hitRate}%`);
    console.log(`Groundedness Score: ${groundednessRate}%`);
    console.log(`Refusal Correctness (Adversarial): ${refusalRate}%`);
    console.log("========================================================\n");
    
    const resultsData = {
        totalPairs: total,
        standardTotal: standardTotal,
        adversarialTotal: totalAdversarial,
        successfulCalls: successfulCalls,
        retrievalHitRate: hitRate,
        groundednessScore: groundednessRate,
        refusalCorrectness: refusalRate
    };
    
    fs.writeFileSync('eval/eval_results.json', JSON.stringify(resultsData, null, 2));
    console.log("Results saved to eval/eval_results.json");
}

runQaEval();
