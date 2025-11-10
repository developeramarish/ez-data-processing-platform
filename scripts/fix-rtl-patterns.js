/**
 * MongoDB Migration Script - Fix RTL-Reversed Patterns
 * 
 * This script unreverses regex patterns, cron expressions, and PromQL queries
 * that were stored in RTL (reversed) format due to Hebrew UI layout.
 * 
 * Usage: node scripts/fix-rtl-patterns.js
 */

const { MongoClient } = require('mongodb');

const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://localhost:27017';
const DATABASE_NAME = 'ezplatform';

/**
 * Unreverse a string if it appears to be RTL-corrupted
 * Checks if string starts with $ or } (indicators of reversed regex/expression)
 */
function unreversePattern(str) {
  if (!str || typeof str !== 'string') return str;
  
  // Check if pattern looks reversed (starts with $ or } instead of ^ or [)
  if (str.startsWith('$') || str.startsWith('}')) {
    const reversed = str.split('').reverse().join('');
    console.log(`  Unre versing: "${str}" → "${reversed}"`);
    return reversed;
  }
  
  return str;
}

/**
 * Recursively fix pattern fields in JSON Schema
 */
function fixPatternsInObject(obj, path = '') {
  let modified = false;
  
  if (!obj || typeof obj !== 'object') return { obj, modified };
  
  if (Array.isArray(obj)) {
    obj.forEach((item, index) => {
      const result = fixPatternsInObject(item, `${path}[${index}]`);
      if (result.modified) {
        obj[index] = result.obj;
        modified = true;
      }
    });
  } else {
    for (const [key, value] of Object.entries(obj)) {
      if (key === 'pattern' && typeof value === 'string') {
        const fixed = unreversePattern(value);
        if (fixed !== value) {
          console.log(`  Fixed pattern at ${path}.${key}`);
          obj[key] = fixed;
          modified = true;
        }
      } else if (typeof value === 'object' && value !== null) {
        const result = fixPatternsInObject(value, `${path}.${key}`);
        if (result.modified) {
          obj[key] = result.obj;
          modified = true;
        }
      }
    }
  }
  
  return { obj, modified };
}

/**
 * Main migration function
 */
async function fixRTLPatterns() {
  console.log('🔧 Starting RTL Pattern Fix Migration...\n');
  console.log(`MongoDB URI: ${MONGODB_URI}`);
  console.log(`Database: ${DATABASE_NAME}\n`);
  
  const client = await MongoClient.connect(MONGODB_URI);
  
  try {
    const db = client.db(DATABASE_NAME);
    
    let totalFixed = 0;
    
    // ====================================
    // 1. Fix Schemas - JSON Schema patterns
    // ====================================
    console.log('📋 Fixing schemas...');
    const schemasCollection = db.collection('schemas');
    const schemas = await schemasCollection.find({}).toArray();
    
    console.log(`Found ${schemas.length} schemas to check`);
    
    for (const schema of schemas) {
      let modified = false;
      
      try {
        // Parse JSON Schema content
        const content = typeof schema.jsonSchemaContent === 'string'
          ? JSON.parse(schema.jsonSchemaContent)
          : schema.jsonSchemaContent;
        
        // Fix patterns recursively
        const result = fixPatternsInObject(content, schema.name);
        
        if (result.modified) {
          // Update database
          await schemasCollection.updateOne(
            { _id: schema._id },
            { 
              $set: { 
                jsonSchemaContent: JSON.stringify(result.obj, null, 2),
                updatedAt: new Date(),
                updatedBy: 'RTL_Migration_Script'
              } 
            }
          );
          
          console.log(`✅ Fixed schema: ${schema.name} (${schema.displayName})`);
          totalFixed++;
        } else {
          console.log(`  Schema OK: ${schema.name}`);
        }
      } catch (error) {
        console.error(`❌ Error processing schema ${schema.name}:`, error.message);
      }
    }
    
    console.log(`\nSchemas: ${totalFixed} fixed\n`);
    
    // ====================================
    // 2. Fix Data Sources - Cron expressions
    // ====================================
    console.log('📁 Fixing data sources...');
    const datasourcesCollection = db.collection('datasources');
    const datasources = await datasourcesCollection.find({}).toArray();
    
    console.log(`Found ${datasources.length} data sources to check`);
    let cronFixed = 0;
    
    for (const ds of datasources) {
      try {
        if (ds.schedule && ds.schedule.cronExpression) {
          const original = ds.schedule.cronExpression;
          const fixed = unreversePattern(original);
          
          if (fixed !== original) {
            await datasourcesCollection.updateOne(
              { _id: ds._id },
              { 
                $set: { 
                  'schedule.cronExpression': fixed,
                  updatedAt: new Date(),
                  updatedBy: 'RTL_Migration_Script'
                } 
              }
            );
            
            console.log(`✅ Fixed data source cron: ${ds.name} (${ds.displayName})`);
            console.log(`  "${original}" → "${fixed}"`);
            cronFixed++;
          }
        }
      } catch (error) {
        console.error(`❌ Error processing data source ${ds.name}:`, error.message);
      }
    }
    
    console.log(`\nData Sources: ${cronFixed} cron expressions fixed\n`);
    
    // ====================================
    // 3. Fix Metrics - PromQL expressions
    // ====================================
    console.log('📊 Fixing metrics...');
    const metricsCollection = db.collection('metrics');
    const metrics = await metricsCollection.find({}).toArray();
    
    console.log(`Found ${metrics.length} metrics to check`);
    let promqlFixed = 0;
    
    for (const metric of metrics) {
      try {
        let modified = false;
        
        if (metric.alertRules && Array.isArray(metric.alertRules)) {
          metric.alertRules.forEach((rule, index) => {
            if (rule.expression) {
              const original = rule.expression;
              const fixed = unreversePattern(original);
              
              if (fixed !== original) {
                rule.expression = fixed;
                modified = true;
                console.log(`  Fixed alert "${rule.name}": "${original}" → "${fixed}"`);
              }
            }
          });
          
          if (modified) {
            await metricsCollection.updateOne(
              { _id: metric._id },
              { 
                $set: { 
                  alertRules: metric.alertRules,
                  updatedAt: new Date(),
                  updatedBy: 'RTL_Migration_Script'
                } 
              }
            );
            
            console.log(`✅ Fixed metric alerts: ${metric.name} (${metric.displayName})`);
            promqlFixed++;
          }
        }
      } catch (error) {
        console.error(`❌ Error processing metric ${metric.name}:`, error.message);
      }
    }
    
    console.log(`\nMetrics: ${promqlFixed} PromQL expressions fixed\n`);
    
    // ====================================
    // Summary
    // ====================================
    console.log('='.repeat(50));
    console.log('✅ RTL Pattern Fix Migration Complete!');
    console.log('='.repeat(50));
    console.log(`📋 Schemas fixed: ${totalFixed}`);
    console.log(`📁 Data source cron fixed: ${cronFixed}`);
    console.log(`📊 Metric PromQL fixed: ${promqlFixed}`);
    console.log(`🎯 Total documents fixed: ${totalFixed + cronFixed + promqlFixed}`);
    console.log('='.repeat(50));
    
  } catch (error) {
    console.error('❌ Migration failed:', error);
    throw error;
  } finally {
    await client.close();
    console.log('\n✅ Database connection closed');
  }
}

// Run migration
if (require.main === module) {
  fixRTLPatterns()
    .then(() => {
      console.log('\n✨ Migration completed successfully!');
      process.exit(0);
    })
    .catch((error) => {
      console.error('\n💥 Migration failed:', error);
      process.exit(1);
    });
}

module.exports = { fixRTLPatterns, unreversePattern };
