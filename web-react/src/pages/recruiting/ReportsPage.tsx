import React, { useState, useEffect } from 'react';
import api from '../../services/api';
import './RecruitingPages.css';

const ReportsPage: React.FC = () => {
  const [pipelineData, setPipelineData] = useState<any[]>([]);
  const [sourceData, setSourceData] = useState<any[]>([]);
  const [timeToHire, setTimeToHire] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadAnalytics();
  }, []);

  const loadAnalytics = async () => {
    try {
      setLoading(true);
      const [pipeline, sources, tth] = await Promise.all([
        api.tenant.get('/recruitment/analytics/pipeline'),
        api.tenant.get('/recruitment/analytics/sources'),
        api.tenant.get('/recruitment/analytics/time-to-hire')
      ]);
      setPipelineData(pipeline.data || []);
      setSourceData(sources.data || []);
      setTimeToHire(tth.data || null);
    } catch (error) {
      console.error('Error loading analytics:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading analytics...</div>;
  }

  return (
    <div className="recruiting-page">
      <div className="page-header">
        <h1>Recruitment Analytics & Reports</h1>
      </div>

      <div className="analytics-grid">
        <div className="analytics-card">
          <h3>Time to Hire</h3>
          {timeToHire && (
            <div className="metric-group">
              <div className="metric">
                <div className="metric-value">{timeToHire.averageDays}</div>
                <div className="metric-label">Average Days</div>
              </div>
              <div className="metric">
                <div className="metric-value">{timeToHire.minDays}</div>
                <div className="metric-label">Min Days</div>
              </div>
              <div className="metric">
                <div className="metric-value">{timeToHire.maxDays}</div>
                <div className="metric-label">Max Days</div>
              </div>
              <div className="metric">
                <div className="metric-value">{timeToHire.totalHired}</div>
                <div className="metric-label">Total Hired</div>
              </div>
            </div>
          )}
        </div>

        <div className="analytics-card">
          <h3>Pipeline Distribution</h3>
          <div className="pipeline-chart">
            {pipelineData.map((item) => (
              <div key={item.stage} className="pipeline-item">
                <div className="pipeline-label">{item.stage}</div>
                <div className="pipeline-bar">
                  <div
                    className="pipeline-fill"
                    style={{ width: `${(item.count / Math.max(...pipelineData.map(p => p.count), 1)) * 100}%` }}
                  />
                </div>
                <div className="pipeline-count">{item.count}</div>
              </div>
            ))}
          </div>
        </div>

        <div className="analytics-card">
          <h3>Source Effectiveness</h3>
          <div className="source-list">
            {sourceData.map((source) => (
              <div key={source.source} className="source-item">
                <div className="source-header">
                  <span className="source-name">{source.source}</span>
                  <span className="source-stats">
                    {source.hiredCount} / {source.count}
                  </span>
                </div>
                <div className="source-bar">
                  <div
                    className="source-fill"
                    style={{ width: `${(source.count / Math.max(...sourceData.map(s => s.count), 1)) * 100}%` }}
                  />
                </div>
                <div className="source-conversion">
                  Conversion: {source.count > 0 ? ((source.hiredCount / source.count) * 100).toFixed(1) : 0}%
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default ReportsPage;
