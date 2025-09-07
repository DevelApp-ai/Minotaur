/**
 * Evaluation Dashboard
 *
 * This system provides comprehensive reporting, visualization, and analysis
 * capabilities for Golem benchmark evaluations. It generates publication-ready
 * reports and interactive dashboards for evaluation results.
 */

import { GolemEvaluationRunner, EvaluationRun, ComparativeAnalysis } from './GolemEvaluationRunner';
import { BenchmarkEvaluationSummary, PassAtKResult, ValidationResult } from './BenchmarkValidator';
import { GolemSolution } from './GolemBenchmarkSolver';
import { promises as fs } from 'fs';
import * as path from 'path';

export interface DashboardConfig {
  outputDirectory: string;
  generateHTML: boolean;
  generateMarkdown: boolean;
  generateJSON: boolean;
  includeCharts: boolean;
  includeDetailedBreakdowns: boolean;
  includeComparativeAnalysis: boolean;
  theme: 'light' | 'dark' | 'professional';
}

export interface EvaluationReport {
  metadata: {
    reportId: string;
    generatedAt: string;
    evaluationRunId: string;
    golemVersion: string;
    reportVersion: string;
  };
  executiveSummary: {
    overallPerformance: string;
    keyFindings: string[];
    recommendations: string[];
    comparisonToBaselines: string;
  };
  benchmarkResults: {
    [benchmark: string]: {
      summary: BenchmarkEvaluationSummary;
      passAtK: PassAtKResult[];
      keyInsights: string[];
      performanceAnalysis: string;
    };
  };
  comparativeAnalysis: ComparativeAnalysis;
  technicalDetails: {
    evaluationConfiguration: any;
    systemPerformance: {
      totalEvaluationTime: number;
      averageGenerationTime: number;
      averageValidationTime: number;
      resourceUtilization: any;
    };
    errorAnalysis: {
      totalErrors: number;
      errorsByCategory: Record<string, number>;
      commonFailurePatterns: string[];
    };
  };
  appendices: {
    rawResults: any;
    detailedMetrics: any;
    benchmarkSpecifications: any;
  };
}

export interface PerformanceChart {
  type: 'bar' | 'line' | 'pie' | 'scatter' | 'heatmap';
  title: string;
  data: any;
  config: any;
}

export class EvaluationDashboard {
  private evaluationRunner: GolemEvaluationRunner;
  private workingDirectory: string;

  constructor(evaluationRunner: GolemEvaluationRunner, workingDir: string = process.cwd()) {
    this.evaluationRunner = evaluationRunner;
    this.workingDirectory = workingDir;
  }

  /**
   * Generate comprehensive evaluation report
   */
  async generateReport(
    evaluationRun: EvaluationRun,
    config: DashboardConfig,
  ): Promise<EvaluationReport> {
    // eslint-disable-next-line no-console
    console.log(`📊 Generating comprehensive evaluation report for run: ${evaluationRun.runId}`);

    if (evaluationRun.status !== 'completed') {
      throw new Error(`Cannot generate report for incomplete evaluation run: ${evaluationRun.status}`);
    }

    // Generate comparative analysis
    const comparativeAnalysis = await this.evaluationRunner.generateComparativeAnalysis();

    // Create comprehensive report
    const report: EvaluationReport = {
      metadata: {
        reportId: `report_${evaluationRun.runId}_${Date.now()}`,
        generatedAt: new Date().toISOString(),
        evaluationRunId: evaluationRun.runId,
        golemVersion: '1.0.0',
        reportVersion: '1.0.0',
      },
      executiveSummary: await this.generateExecutiveSummary(evaluationRun, comparativeAnalysis),
      benchmarkResults: await this.generateBenchmarkResults(evaluationRun),
      comparativeAnalysis,
      technicalDetails: await this.generateTechnicalDetails(evaluationRun),
      appendices: await this.generateAppendices(evaluationRun),
    };

    // Export report in requested formats
    await this.exportReport(report, config);

    // eslint-disable-next-line no-console
    console.log(`✅ Comprehensive evaluation report generated: ${report.metadata.reportId}`);
    return report;
  }

  /**
   * Generate executive summary
   */
  private async generateExecutiveSummary(
    evaluationRun: EvaluationRun,
    // eslint-disable-next-line max-len
    comparativeAnalysis: ComparativeAnalysis,
  ): Promise<EvaluationReport['executiveSummary']> {

    const overallStats = evaluationRun.results.overallStats;
    const ranking = comparativeAnalysis.analysis.golemRanking;

    // Determine performance level
    let performanceLevel: string;
    if (overallStats.overallSuccessRate >= 70) {
      performanceLevel = 'Excellent';
    } else if (overallStats.overallSuccessRate >= 50) {
      performanceLevel = 'Good';
    } else if (overallStats.overallSuccessRate >= 30) {
      performanceLevel = 'Fair';
    } else {
      performanceLevel = 'Needs Improvement';
    }

    const overallPerformance = `Golem achieved a ${performanceLevel.toLowerCase()} overall performance with a ${overallStats.overallSuccessRate.toFixed(1)}% success rate across ${overallStats.totalProblems} benchmark problems. The system ranks #${ranking} among evaluated models, demonstrating ${ranking <= 2 ? 'state-of-the-art' : ranking <= 4 ? 'competitive' : 'developing'} capabilities in automated code generation and problem solving.`;

    // Generate key findings
    const keyFindings: string[] = [
      // eslint-disable-next-line max-len
      `Overall Success Rate: ${overallStats.overallSuccessRate.toFixed(1)}% (${overallStats.totalSolutions} solutions generated)`,
      `Average Solution Confidence: ${overallStats.averageConfidence.toFixed(2)} (scale: 0-1)`,
      `Average Generation Time: ${overallStats.averageGenerationTime.toFixed(0)}ms per solution`,
      `Benchmark Ranking: #${ranking} among compared models (GPT-4, Claude, Codex)`,
    ];

    // Add benchmark-specific findings
    for (const [benchmark, summary] of evaluationRun.results.benchmarkResults) {
      if (summary.overallSuccessRate > 60) {
        keyFindings.push(`Strong performance on ${benchmark}: ${summary.overallSuccessRate.toFixed(1)}% success rate`);
      } else if (summary.overallSuccessRate < 20) {
        // eslint-disable-next-line max-len
        keyFindings.push(`Challenging performance on ${benchmark}: ${summary.overallSuccessRate.toFixed(1)}% success rate`);
      }
    }

    // Generate recommendations
    const recommendations: string[] = [...comparativeAnalysis.analysis.recommendations];

    if (overallStats.averageGenerationTime > 30000) {
      recommendations.push('Optimize solution generation speed for better user experience');
    }

    if (overallStats.averageConfidence < 0.7) {
      recommendations.push('Improve confidence scoring accuracy for better solution quality assessment');
    }

    // Add benchmark-specific recommendations
    for (const benchmark of comparativeAnalysis.analysis.weaknessBenchmarks) {
      recommendations.push(`Focus improvement efforts on ${benchmark} benchmark challenges`);
    }

    const comparisonToBaselines = `Golem's performance places it ${ranking === 1 ? 'at the top of' : `in position #${ranking} among`} the evaluated models. ${comparativeAnalysis.analysis.strengthBenchmarks.length > 0 ? `It shows particular strength in ${comparativeAnalysis.analysis.strengthBenchmarks.join(', ')} benchmarks.` : ''} ${comparativeAnalysis.analysis.weaknessBenchmarks.length > 0 ? `Areas for improvement include ${comparativeAnalysis.analysis.weaknessBenchmarks.join(', ')}.` : ''}`;

    return {
      overallPerformance,
      keyFindings,
      recommendations,
      comparisonToBaselines,
    };
  }

  /**
   * Generate benchmark results section
   */
  private async generateBenchmarkResults(
    evaluationRun: EvaluationRun,
  ): Promise<EvaluationReport['benchmarkResults']> {
    // eslint-disable-next-line max-len

    const benchmarkResults: EvaluationReport['benchmarkResults'] = {};

    for (const [benchmark, summary] of evaluationRun.results.benchmarkResults) {
      const passAtKResults = evaluationRun.results.passAtKResults.get(benchmark) || [];

      // Generate key insights
      const keyInsights: string[] = [
        `Solved ${summary.validatedSolutions} out of ${summary.totalProblems} problems`,
        // eslint-disable-next-line max-len
        `Pass@1: ${summary.passAt1.toFixed(1)}%, Pass@5: ${summary.passAt5.toFixed(1)}%, Pass@10: ${summary.passAt10.toFixed(1)}%`,
        `Average solution score: ${summary.averageScore.toFixed(2)}/1.0`,
        `Average execution time: ${summary.averageExecutionTime.toFixed(0)}ms`,
      ];

      // Add difficulty-based insights
      const difficulties = Object.keys(summary.byDifficulty);
      if (difficulties.length > 1) {
        const bestDifficulty = difficulties.reduce((best, current) =>
          summary.byDifficulty[current].successRate > summary.byDifficulty[best].successRate ? current : best,
        );
        const worstDifficulty = difficulties.reduce((worst, current) =>
          summary.byDifficulty[current].successRate < summary.byDifficulty[worst].successRate ? current : worst,
        );

        // eslint-disable-next-line max-len
        keyInsights.push(`Best performance on ${bestDifficulty} problems: ${summary.byDifficulty[bestDifficulty].successRate.toFixed(1)}%`);
        // eslint-disable-next-line max-len
        keyInsights.push(`Most challenging: ${worstDifficulty} problems: ${summary.byDifficulty[worstDifficulty].successRate.toFixed(1)}%`);
      }

      // Add approach-based insights
      const approaches = Object.keys(summary.byApproach);
      if (approaches.length > 1) {
        const bestApproach = approaches.reduce((best, current) =>
          summary.byApproach[current].successRate > summary.byApproach[best].successRate ? current : best,
        );
        // eslint-disable-next-line max-len
        keyInsights.push(`Most effective approach: ${bestApproach} (${summary.byApproach[bestApproach].successRate.toFixed(1)}% success rate)`);
      }

      // Generate performance analysis
      let performanceAnalysis = `The ${benchmark} benchmark evaluation demonstrates `;

      if (summary.overallSuccessRate >= 70) {
        performanceAnalysis += `excellent performance with a ${summary.overallSuccessRate.toFixed(1)}% success rate. `;
      } else if (summary.overallSuccessRate >= 50) {
        performanceAnalysis += `solid performance with a ${summary.overallSuccessRate.toFixed(1)}% success rate. `;
      } else if (summary.overallSuccessRate >= 30) {
        performanceAnalysis += `moderate performance with a ${summary.overallSuccessRate.toFixed(1)}% success rate. `;
      } else {
        // eslint-disable-next-line max-len
        performanceAnalysis += `challenging results with a ${summary.overallSuccessRate.toFixed(1)}% success rate, indicating opportunities for improvement. `;
      }

      // eslint-disable-next-line max-len
      performanceAnalysis += `The system processed ${summary.totalProblems} problems with an average execution time of ${summary.averageExecutionTime.toFixed(0)}ms per solution. `;

      if (summary.performanceMetrics.errorRate > 20) {
        // eslint-disable-next-line max-len
        performanceAnalysis += `Error rate of ${summary.performanceMetrics.errorRate.toFixed(1)}% suggests need for improved error handling. `;
      } else if (summary.performanceMetrics.errorRate < 5) {
        // eslint-disable-next-line max-len
        performanceAnalysis += `Low error rate of ${summary.performanceMetrics.errorRate.toFixed(1)}% indicates robust solution generation. `;
      }

      benchmarkResults[benchmark] = {
        summary,
        passAtK: passAtKResults,
        keyInsights,
        performanceAnalysis,
      };
    }

    return benchmarkResults;
  }

  /**
   * Generate technical details section
   */
  private async generateTechnicalDetails(
    evaluationRun: EvaluationRun,
  ): Promise<EvaluationReport['technicalDetails']> {

    const totalEvaluationTime = evaluationRun.endTime ?
      evaluationRun.endTime - evaluationRun.startTime : 0;

    // Analyze errors
    const errorsByCategory: Record<string, number> = {};
    const commonFailurePatterns: string[] = [];

    for (const error of evaluationRun.errors) {
      // Simple error categorization
      if (error.includes('timeout')) {
        errorsByCategory['Timeout'] = (errorsByCategory['Timeout'] || 0) + 1;
      } else if (error.includes('syntax')) {
        errorsByCategory['Syntax Error'] = (errorsByCategory['Syntax Error'] || 0) + 1;
      } else if (error.includes('validation')) {
        errorsByCategory['Validation Error'] = (errorsByCategory['Validation Error'] || 0) + 1;
      } else if (error.includes('generation')) {
        errorsByCategory['Generation Error'] = (errorsByCategory['Generation Error'] || 0) + 1;
      } else {
        errorsByCategory['Other'] = (errorsByCategory['Other'] || 0) + 1;
      }
    }

    // Identify common failure patterns
    if (errorsByCategory['Timeout'] > 5) {
      commonFailurePatterns.push('Frequent timeout issues suggest need for optimization');
    }
    if (errorsByCategory['Syntax Error'] > 10) {
      commonFailurePatterns.push('High syntax error rate indicates code generation quality issues');
    }
    if (errorsByCategory['Validation Error'] > 5) {
      commonFailurePatterns.push('Validation errors suggest test execution environment issues');
    }
    // eslint-disable-next-line max-len

    return {
      evaluationConfiguration: evaluationRun.config,
      systemPerformance: {
    // eslint-disable-next-line max-len
        totalEvaluationTime,
        averageGenerationTime: evaluationRun.results.overallStats.averageGenerationTime,
        averageValidationTime: evaluationRun.results.overallStats.averageValidationTime,
        resourceUtilization: {
          memoryEfficiency: 'Good', // Would need actual measurements
          cpuUtilization: 'Moderate',
          diskUsage: 'Low',
        },
      },
      errorAnalysis: {
        totalErrors: evaluationRun.errors.length,
        errorsByCategory,
        commonFailurePatterns,
      },
    };
  }

  /**
   * Generate appendices section
   */
  private async generateAppendices(
    evaluationRun: EvaluationRun,
  ): Promise<EvaluationReport['appendices']> {

    return {
      rawResults: {
        benchmarkResults: Object.fromEntries(evaluationRun.results.benchmarkResults),
        passAtKResults: Object.fromEntries(evaluationRun.results.passAtKResults),
        overallStats: evaluationRun.results.overallStats,
      },
      detailedMetrics: {
        evaluationProgress: evaluationRun.progress,
        errors: evaluationRun.errors,
        warnings: evaluationRun.warnings,
      },
      benchmarkSpecifications: {
        'swe-bench': 'GitHub issue resolution with patch generation',
        'quixbugs': 'Single-line bug repair in classic algorithms',
        'fim': 'Fill-in-the-middle code completion tasks',
        'mbpp': 'Mostly Basic Python Programming problems',
        'humaneval': 'Function implementation from docstrings',
      },
    };
  }

  /**
   * Export report in multiple formats
   */
  private async exportReport(
    report: EvaluationReport,
    config: DashboardConfig,
  ): Promise<void> {

    // Ensure output directory exists
    await fs.mkdir(config.outputDirectory, { recursive: true });

    // Export JSON format
    if (config.generateJSON) {
      const jsonPath = path.join(config.outputDirectory, `${report.metadata.reportId}.json`);
      await fs.writeFile(jsonPath, JSON.stringify(report, null, 2));
    // eslint-disable-next-line no-console
      console.log(`  📄 Generated JSON report: ${jsonPath}`);
    }

    // Export Markdown format
    if (config.generateMarkdown) {
      const markdownContent = await this.generateMarkdownReport(report);
      const markdownPath = path.join(config.outputDirectory, `${report.metadata.reportId}.md`);
      await fs.writeFile(markdownPath, markdownContent);
    // eslint-disable-next-line no-console
      console.log(`  📄 Generated Markdown report: ${markdownPath}`);
    }

    // Export HTML format
    if (config.generateHTML) {
      const htmlContent = await this.generateHTMLReport(report, config);
      const htmlPath = path.join(config.outputDirectory, `${report.metadata.reportId}.html`);
      await fs.writeFile(htmlPath, htmlContent);
    // eslint-disable-next-line no-console
      console.log(`  📄 Generated HTML report: ${htmlPath}`);
    }

    // Generate charts if requested
    if (config.includeCharts) {
      await this.generateCharts(report, config);
    }
  }

  /**
   * Generate Markdown report
   */
  private async generateMarkdownReport(report: EvaluationReport): Promise<string> {
    const md = [];

    // Title and metadata
    md.push('# Golem Benchmark Evaluation Report');
    md.push(`**Report ID:** ${report.metadata.reportId}`);
    md.push(`**Generated:** ${new Date(report.metadata.generatedAt).toLocaleString()}`);
    md.push(`**Evaluation Run:** ${report.metadata.evaluationRunId}`);
    md.push(`**Golem Version:** ${report.metadata.golemVersion}`);
    md.push('');

    // Executive Summary
    md.push('## Executive Summary');
    md.push('');
    md.push(report.executiveSummary.overallPerformance);
    md.push('');

    md.push('### Key Findings');
    for (const finding of report.executiveSummary.keyFindings) {
      md.push(`- ${finding}`);
    }
    md.push('');

    md.push('### Recommendations');
    for (const recommendation of report.executiveSummary.recommendations) {
      md.push(`- ${recommendation}`);
    }
    md.push('');

    md.push('### Comparison to Baselines');
    md.push(report.executiveSummary.comparisonToBaselines);
    md.push('');

    // Benchmark Results
    md.push('## Benchmark Results');
    md.push('');

    for (const [benchmark, results] of Object.entries(report.benchmarkResults)) {
      md.push(`### ${benchmark.toUpperCase()} Benchmark`);
      md.push('');

      md.push('**Performance Analysis:**');
      md.push(results.performanceAnalysis);
      md.push('');

      md.push('**Key Insights:**');
      for (const insight of results.keyInsights) {
        md.push(`- ${insight}`);
      }
      md.push('');

      md.push('**Pass@k Results:**');
      md.push('| k | Pass Rate | Confidence Interval |');
      md.push('|---|-----------|---------------------|');
      for (const passAtK of results.passAtK) {
        // eslint-disable-next-line max-len
        const ci = `[${passAtK.metadata.confidenceInterval[0].toFixed(1)}%, ${passAtK.metadata.confidenceInterval[1].toFixed(1)}%]`;
        md.push(`| ${passAtK.k} | ${passAtK.passAtK.toFixed(1)}% | ${ci} |`);
      }
      md.push('');
    }

    // Comparative Analysis
    md.push('## Comparative Analysis');
    md.push('');

    md.push('### Golem vs. Published Baselines');
    md.push('| Model | Overall Score | HumanEval | MBPP | SWE-bench |');
    md.push('|-------|---------------|-----------|------|-----------|');

    const golem = report.comparativeAnalysis.golemResults;
    md.push(`| **Golem** | **${golem.overallScore.toFixed(1)}%** | **${golem.byBenchmark.humaneval?.toFixed(1) || 'N/A'}%** | **${golem.byBenchmark.mbpp?.toFixed(1) || 'N/A'}%** | **${golem.byBenchmark['swe-bench']?.toFixed(1) || 'N/A'}%** |`);

    for (const [model, scores] of Object.entries(report.comparativeAnalysis.publishedBaselines)) {
      md.push(`| ${model.toUpperCase()} | ${scores.overall?.toFixed(1) || 'N/A'}% | ${scores.humaneval?.toFixed(1) || 'N/A'}% | ${scores.mbpp?.toFixed(1) || 'N/A'}% | ${scores['swe-bench']?.toFixed(1) || 'N/A'}% |`);
    }
    md.push('');

    md.push('**Analysis:**');
    md.push(`- Golem ranks #${report.comparativeAnalysis.analysis.golemRanking} among evaluated models`);
    if (report.comparativeAnalysis.analysis.strengthBenchmarks.length > 0) {
      md.push(`- Strong performance on: ${report.comparativeAnalysis.analysis.strengthBenchmarks.join(', ')}`);
    }
    if (report.comparativeAnalysis.analysis.weaknessBenchmarks.length > 0) {
      md.push(`- Areas for improvement: ${report.comparativeAnalysis.analysis.weaknessBenchmarks.join(', ')}`);
    }
    md.push('');

    // Technical Details
    md.push('## Technical Details');
    md.push('');

    const tech = report.technicalDetails;
    md.push('**System Performance:**');
    md.push(`- Total evaluation time: ${(tech.systemPerformance.totalEvaluationTime / 1000).toFixed(0)} seconds`);
    md.push(`- Average generation time: ${tech.systemPerformance.averageGenerationTime.toFixed(0)}ms`);
    md.push(`- Average validation time: ${tech.systemPerformance.averageValidationTime.toFixed(0)}ms`);
    md.push('');

    md.push('**Error Analysis:**');
    md.push(`- Total errors: ${tech.errorAnalysis.totalErrors}`);
    for (const [category, count] of Object.entries(tech.errorAnalysis.errorsByCategory)) {
      md.push(`- ${category}: ${count}`);
    }
    md.push('');

    if (tech.errorAnalysis.commonFailurePatterns.length > 0) {
      md.push('**Common Failure Patterns:**');
      for (const pattern of tech.errorAnalysis.commonFailurePatterns) {
        md.push(`- ${pattern}`);
      }
      md.push('');
    }

    return md.join('\n');
  }

  /**
   * Generate HTML report
   */
  private async generateHTMLReport(report: EvaluationReport, config: DashboardConfig): Promise<string> {
    const theme = config.theme || 'professional';

    const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Golem Benchmark Evaluation Report</title>
    <style>
        ${this.getHTMLStyles(theme)}
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>Golem Benchmark Evaluation Report</h1>
            <div class="metadata">
                <p><strong>Report ID:</strong> ${report.metadata.reportId}</p>
                <p><strong>Generated:</strong> ${new Date(report.metadata.generatedAt).toLocaleString()}</p>
                <p><strong>Evaluation Run:</strong> ${report.metadata.evaluationRunId}</p>
                <p><strong>Golem Version:</strong> ${report.metadata.golemVersion}</p>
            </div>
        </header>

        <section class="executive-summary">
            <h2>Executive Summary</h2>
            <p class="overview">${report.executiveSummary.overallPerformance}</p>
            
            <div class="findings">
                <h3>Key Findings</h3>
                <ul>
                    ${report.executiveSummary.keyFindings.map(finding => `<li>${finding}</li>`).join('')}
                </ul>
            </div>

            <div class="recommendations">
                <h3>Recommendations</h3>
                <ul>
                    ${report.executiveSummary.recommendations.map(rec => `<li>${rec}</li>`).join('')}
                </ul>
            </div>

            <div class="comparison">
                <h3>Comparison to Baselines</h3>
                <p>${report.executiveSummary.comparisonToBaselines}</p>
            </div>
        </section>

        <section class="benchmark-results">
            <h2>Benchmark Results</h2>
            ${Object.entries(report.benchmarkResults).map(([benchmark, results]) => `
                <div class="benchmark">
                    <h3>${benchmark.toUpperCase()} Benchmark</h3>
                    <p class="analysis">${results.performanceAnalysis}</p>
                    
                    <div class="insights">
                        <h4>Key Insights</h4>
                        <ul>
    // eslint-disable-next-line max-len
                            ${results.keyInsights.map(insight => `<li>${insight}</li>`).join('')}
                        </ul>
                    </div>

                    <div class="pass-at-k">
                        <h4>Pass@k Results</h4>
                        <table>
                            <thead>
                                <tr>
                                    <th>k</th>
                                    <th>Pass Rate</th>
                                    <th>Confidence Interval</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${results.passAtK.map(passAtK => `
                                    <tr>
                                        <td>${passAtK.k}</td>
                                        <td>${passAtK.passAtK.toFixed(1)}%</td>
                                        // eslint-disable-next-line max-len
                                        <td>[${passAtK.metadata.confidenceInterval[0].toFixed(1)}%, ${passAtK.metadata.confidenceInterval[1].toFixed(1)}%]</td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>
            `).join('')}
        </section>

        <section class="comparative-analysis">
            <h2>Comparative Analysis</h2>
            <div class="comparison-table">
                <h3>Golem vs. Published Baselines</h3>
                <table>
                    <thead>
                        <tr>
                            <th>Model</th>
                            <th>Overall Score</th>
                            <th>HumanEval</th>
                            <th>MBPP</th>
                            <th>SWE-bench</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr class="golem-row">
                            <td><strong>Golem</strong></td>
                            // eslint-disable-next-line max-len
                            <td><strong>${report.comparativeAnalysis.golemResults.overallScore.toFixed(1)}%</strong></td>
                            <td><strong>${report.comparativeAnalysis.golemResults.byBenchmark.humaneval?.toFixed(1) || 'N/A'}%</strong></td>
                            <td><strong>${report.comparativeAnalysis.golemResults.byBenchmark.mbpp?.toFixed(1) || 'N/A'}%</strong></td>
                            <td><strong>${report.comparativeAnalysis.golemResults.byBenchmark['swe-bench']?.toFixed(1) || 'N/A'}%</strong></td>
                        </tr>
                        ${Object.entries(report.comparativeAnalysis.publishedBaselines).map(([model, scores]) => `
                            <tr>
                                <td>${model.toUpperCase()}</td>
                                <td>${scores.overall?.toFixed(1) || 'N/A'}%</td>
                                <td>${scores.humaneval?.toFixed(1) || 'N/A'}%</td>
                                <td>${scores.mbpp?.toFixed(1) || 'N/A'}%</td>
                                <td>${scores['swe-bench']?.toFixed(1) || 'N/A'}%</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        </section>

        <footer>
            <p>Generated by Golem Evaluation System v${report.metadata.reportVersion}</p>
        </footer>
    </div>
</body>
</html>
    `;

    return html;
  }

  /**
   * Get HTML styles based on theme
   */
  private getHTMLStyles(theme: string): string {
    const baseStyles = `
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; }
        .container { max-width: 1200px; margin: 0 auto; padding: 20px; }
        header { margin-bottom: 30px; border-bottom: 2px solid #e0e0e0; padding-bottom: 20px; }
        h1, h2, h3, h4 { margin-bottom: 15px; }
        h1 { font-size: 2.5em; }
        h2 { font-size: 2em; margin-top: 30px; }
        h3 { font-size: 1.5em; margin-top: 25px; }
        h4 { font-size: 1.2em; margin-top: 20px; }
        p { margin-bottom: 15px; }
        ul, ol { margin-left: 20px; margin-bottom: 15px; }
        li { margin-bottom: 5px; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
        th { font-weight: 600; }
    // eslint-disable-next-line max-len
        .metadata { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 10px; margin-top: 15px; }
        .benchmark { margin-bottom: 40px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; }
        .golem-row { background-color: #f0f8ff; }
        footer { margin-top: 50px; padding-top: 20px; border-top: 1px solid #e0e0e0; text-align: center; color: #666; }
    `;

    if (theme === 'dark') {
      return baseStyles + `
        body { background-color: #1a1a1a; color: #e0e0e0; }
        header { border-bottom-color: #444; }
        .benchmark { border-color: #444; background-color: #2a2a2a; }
        th, td { border-bottom-color: #444; }
        .golem-row { background-color: #2a3a4a; }
        footer { border-top-color: #444; }
      `;
    } else if (theme === 'professional') {
      return baseStyles + `
        body { background-color: #fafafa; color: #333; }
        header { background-color: #fff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .benchmark { background-color: #fff; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        table { box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
        th { background-color: #f8f9fa; }
        .golem-row { background-color: #e3f2fd; }
      `;
    }

    return baseStyles;
  }

  /**
   * Generate performance charts
   */
  private async generateCharts(report: EvaluationReport, config: DashboardConfig): Promise<void> {
    const chartsDir = path.join(config.outputDirectory, 'charts');
    await fs.mkdir(chartsDir, { recursive: true });

    // Generate chart data (would integrate with actual charting library)
    const charts: PerformanceChart[] = [
      {
        type: 'bar',
        title: 'Pass@1 Performance by Benchmark',
        data: Object.entries(report.benchmarkResults).map(([benchmark, results]) => ({
          benchmark: benchmark.toUpperCase(),
          passAt1: results.summary.passAt1,
        })),
        config: { yAxis: { title: 'Pass@1 (%)', min: 0, max: 100 } },
      },
      {
        type: 'line',
        title: 'Pass@k Performance Comparison',
        data: Object.entries(report.benchmarkResults).map(([benchmark, results]) => ({
          benchmark: benchmark.toUpperCase(),
          passAt1: results.summary.passAt1,
          passAt5: results.summary.passAt5,
          passAt10: results.summary.passAt10,
        })),
        config: { yAxis: { title: 'Pass Rate (%)', min: 0, max: 100 } },
      },
    ];

    // Export chart configurations (would generate actual charts with a library like Chart.js or D3)
    for (let i = 0; i < charts.length; i++) {
      const chartPath = path.join(chartsDir, `chart_${i + 1}.json`);
      await fs.writeFile(chartPath, JSON.stringify(charts[i], null, 2));
    }

    // eslint-disable-next-line no-console
    console.log(`  📊 Generated ${charts.length} chart configurations in ${chartsDir}`);
  }

  /**
   * Generate quick summary for console output
   */
  generateQuickSummary(evaluationRun: EvaluationRun): string {
    if (evaluationRun.status !== 'completed') {
      return `Evaluation ${evaluationRun.runId} is ${evaluationRun.status}`;
    }

    const stats = evaluationRun.results.overallStats;
    const duration = evaluationRun.endTime ?
      ((evaluationRun.endTime - evaluationRun.startTime) / 1000).toFixed(0) : 'N/A';

    return `
🎯 Evaluation ${evaluationRun.runId} Summary:
📊 Overall Success Rate: ${stats.overallSuccessRate.toFixed(1)}%
🧠 Solutions Generated: ${stats.totalSolutions}
⏱️  Total Time: ${duration}s
📈 Average Confidence: ${stats.averageConfidence.toFixed(2)}
🔧 Average Generation Time: ${stats.averageGenerationTime.toFixed(0)}ms
✅ Benchmarks Evaluated: ${evaluationRun.results.benchmarkResults.size}
    `.trim();
  }

  /**
   * Create default dashboard configuration
   */
  static createDefaultConfig(outputDir: string): DashboardConfig {
    return {
      outputDirectory: outputDir,
      generateHTML: true,
      generateMarkdown: true,
      generateJSON: true,
      includeCharts: true,
      includeDetailedBreakdowns: true,
      includeComparativeAnalysis: true,
      theme: 'professional',
    };
  }
}

