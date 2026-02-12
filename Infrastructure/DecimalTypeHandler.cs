using System;
using System.Data;
using Dapper;

namespace GestaoAulas.Infrastructure
{
    public class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
    {
        public override decimal Parse(object value)
        {
            try 
            {
                if (value == null || value is DBNull) return 0m;
                
                // SQLite costuma retornar double (REAL) ou string (TEXT)
                if (value is double d) return (decimal)d;
                if (value is float f) return (decimal)f;
                if (value is int i) return (decimal)i;
                if (value is long l) return (decimal)l;
                
                if (value is string s) 
                {
                    if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v)) 
                        return v;
                }
                
                return Convert.ToDecimal(value);
            }
            catch
            {
                return 0m;
            }
        }

        public override void SetValue(IDbDataParameter parameter, decimal value)
        {
            parameter.Value = value;
        }
    }
}
