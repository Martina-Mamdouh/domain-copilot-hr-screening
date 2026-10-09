const fs = require('fs');

async function runEval() {
    console.log("Loading ground truth data...");
    const rawData = fs.readFileSync('eval/ground_truth.json');
    const groundTruth = JSON.parse(rawData);

    // Get auth token first
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
    let totalScoreError = 0;
    let decisionMatches = 0;
    let total = groundTruth.length;

    console.log(`Starting evaluation for ${total} candidates...\n`);

    for (let i = 0; i < total; i++) {
        const item = groundTruth[i];
        console.log(`Evaluating [${i+1}/${total}]: ${item.doc_id} (${item.role})`);
        
        try {
            const res = await fetch('http://localhost:8080/api/screening/evaluate', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    candidateDocId: item.doc_id,
                    targetJdId: item.jd
                })
            });

            if (!res.ok) {
                console.error(`  [!] API Error: ${res.status} ${res.statusText}`);
                continue;
            }

            const result = await res.json();
            
            // Map decision
            const statusMap = { 1: "REJECT", 2: "HOLD", 3: "SHORTLIST" };
            const actualDecision = statusMap[result.recommendedDecision] || "UNKNOWN";
            
            // The API returns a score out of 100, ground truth expects out of 5.
            const mappedScore = result.weightedScore / 20.0;
            
            const scoreDiff = Math.abs(mappedScore - item.weighted_score);
            totalScoreError += scoreDiff;
            
            const decisionMatch = actualDecision === item.expected_decision;
            if (decisionMatch) decisionMatches++;

            console.log(`  -> Actual: ${actualDecision} | Expected: ${item.expected_decision} | Match: ${decisionMatch ? '✅' : '❌'}`);
            console.log(`  -> Actual Score: ${mappedScore.toFixed(2)} | Expected: ${item.weighted_score} | Diff: ${scoreDiff.toFixed(2)}`);
            
        } catch (e) {
            console.error(`  [!] Exception evaluating ${item.doc_id}:`, e.message);
        }
    }

    console.log("\n================ EVALUATION SUMMARY ================");
    console.log(`Total Candidates Evaluated: ${total}`);
    console.log(`Decision Accuracy: ${(decisionMatches / total * 100).toFixed(1)}% (${decisionMatches}/${total})`);
    console.log(`Mean Absolute Error (Score): ${(totalScoreError / total).toFixed(2)} points`);
    console.log("====================================================\n");
}

runEval();
