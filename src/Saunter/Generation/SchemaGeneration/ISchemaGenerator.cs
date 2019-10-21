using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Saunter.AsyncApiSchema.v2;
using Saunter.Utils;

namespace Saunter.Generation.SchemaGeneration
{
    public interface ISchemaGenerator
    {
        /// <summary>
        /// Generate a schema, save it in the <paramref name="schemaRepository"/>, and return a reference to it.
        /// </summary>
        ISchema GenerateSchema(Type type, ISchemaRepository schemaRepository);
    }
    
    public class SchemaGenerator : ISchemaGenerator
    {
        private readonly AsyncApiDocumentGeneratorOptions _options;

        public SchemaGenerator(IOptions<AsyncApiDocumentGeneratorOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }
        
        public ISchema GenerateSchema(Type type, ISchemaRepository schemaRepository)
        {
            var schemaId = _options.SchemaIdSelector(type);

            var reference = schemaRepository.GetOrAdd(type, schemaId, () => TypeSchemaFactory(type, schemaRepository));

            return reference;
        }

        private Schema TypeSchemaFactory(Type type, ISchemaRepository schemaRepository)
        {
            var schema = GetSchemaIfPrimitive(type);
            if (schema != null)
            {
                return schema;
            }

            schema = GetSchemaIfEnumerable(type, schemaRepository);
            if (schema != null)
            {
                 return schema;
            }

            schema = new Schema
            {
                Properties = new Dictionary<string, ISchema>()
            };
            
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                var propName = GetPropName(prop);
                var propType = prop.PropertyType;
                
                ISchema propSchema = GetSchemaIfPrimitive(propType);
                
                if (propSchema == null)
                {
                    propSchema = GetSchemaIfEnumerable(propType, schemaRepository);

                    if (propSchema == null)
                    {
                        propSchema = GenerateSchema(propType, schemaRepository);
                    }
                }

                schema.Properties.Add(propName, propSchema);
            }

            return schema;
        }
        
        public string GetPropName(PropertyInfo prop)
        {
            var jsonPropertyAttribute = prop.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute?.PropertyName != null)
            {
                return jsonPropertyAttribute.PropertyName;
            }

            var dataMemberAttribute = prop.GetCustomAttribute<DataMemberAttribute>();
            if (dataMemberAttribute?.Name != null)
            {
                return dataMemberAttribute.Name;
            }

            return _options.PropertyNameSelector(prop);
        }


        private Schema GetSchemaIfPrimitive(Type type)
        {
            if (type.IsInteger())
            {
                return new Schema { Type = "integer" };
            }
            
            if (type.IsNumber())
            {
                return new Schema { Type = "number" };
            }

            if (type == typeof(string))
            {
                return new Schema { Type = "string" };
            }

            if (type.IsBoolean())
            {
                return new Schema { Type = "boolean" };
            }

            if (type.IsEnum(out var members))
            {
                return new Schema
                {
                    Type = "string",
                    Enum = members,
                };
            }
            
            if (type.IsDateTime())
            {
                return new Schema
                {
                    Type = "string",
                    Format = "date-time",
                };
            }

            return null;
        }

        private Schema GetSchemaIfEnumerable(Type type, ISchemaRepository schemaRepository)
        {
            if (type.IsEnumerable(out var elementType))
            {
                var schema = new Schema
                {
                    Type = "array",
                    Items = GenerateSchema(elementType, schemaRepository),
                };

                return schema;
            }

            return null;
        }
        
        
        
        
    }
    
}