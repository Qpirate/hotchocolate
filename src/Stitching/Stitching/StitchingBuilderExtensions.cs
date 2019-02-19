using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Stitching.Merge;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching
{
    public static class StitchingBuilderExtensions
    {
        private static readonly string _introspectionQuery =
            Resources.IntrospectionQuery;

        public static IStitchingBuilder AddSchemaFromString(
            this IStitchingBuilder builder,
            NameString name,
            string schema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentException(
                    Resources.Schema_EmptyOrNull,
                    nameof(schema));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s => Parser.Default.Parse(schema));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromFile(
            this IStitchingBuilder builder,
            NameString name,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    Resources.SchemaFilePath_EmptyOrNull,
                    nameof(path));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
                Parser.Default.Parse(
                    File.ReadAllText(path)));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromHttp(
            this IStitchingBuilder builder,
            NameString name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
            {
                HttpClient httpClient =
                    s.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(name);

                var request = new RemoteQueryRequest
                {
                    Query = _introspectionQuery
                };

                var queryClient = new HttpQueryClient();
                string result = Task.Factory.StartNew(
                    () => queryClient.FetchStringAsync(request, httpClient))
                    .Unwrap().GetAwaiter().GetResult();
                return IntrospectionDeserializer.Deserialize(result);
            });

            return builder;
        }

        public static IStitchingBuilder AddExtensionsFromFile(
            this IStitchingBuilder builder,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    Resources.ExtensionsFilePath_EmptyOrNull,
                    nameof(path));
            }

            builder.AddExtensions(s =>
                Parser.Default.Parse(
                    File.ReadAllText(path)));
            return builder;
        }

        public static IStitchingBuilder AddExtensionsFromString(
            this IStitchingBuilder builder,
            string extensions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(extensions))
            {
                throw new ArgumentException(
                    Resources.Extensions_EmptyOrNull,
                    nameof(extensions));
            }

            builder.AddExtensions(s => Parser.Default.Parse(extensions));
            return builder;
        }

        public static IStitchingBuilder IgnoreRootTypes(
           this IStitchingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddRewriter(
                new RemoveRootTypeRewriter());
        }

        public static IStitchingBuilder IgnoreRootTypes(
            this IStitchingBuilder builder,
            NameString schemaName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return builder.AddRewriter(
                new RemoveRootTypeRewriter(schemaName));
        }

        public static IStitchingBuilder IgnoreType(
            this IStitchingBuilder builder,
            NameString typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            typeName.EnsureNotEmpty(nameof(typeName));

            return builder.AddRewriter(
                new RemoveTypeRewriter(typeName));
        }

        public static IStitchingBuilder IgnoreType(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            typeName.EnsureNotEmpty(nameof(typeName));

            return builder.AddRewriter(
                new RemoveTypeRewriter(schemaName, typeName));
        }

        public static IStitchingBuilder IgnoreField(
            this IStitchingBuilder builder,
            NameString schemaName,
            FieldReference field)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return builder.AddRewriter(
                new RemoveFieldRewriter(schemaName, field));
        }

        public static IStitchingBuilder RenameType(
            this IStitchingBuilder builder,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return builder.AddRewriter(
                new RenameTypeRewriter(originalTypeName, newTypeName));
        }

        public static IStitchingBuilder RenameType(
            this IStitchingBuilder builder,
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return builder.AddRewriter(
                new RenameTypeRewriter(
                    schemaName, originalTypeName, newTypeName));
        }

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            FieldReference field,
            NameString newFieldName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return builder.AddRewriter(
                new RenameFieldRewriter(
                    field, newFieldName));
        }

        public static IStitchingBuilder RenameField(
            this IStitchingBuilder builder,
            NameString schemaName,
            FieldReference field,
            NameString newFieldName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return builder.AddRewriter(
                new RenameFieldRewriter(
                    schemaName, field, newFieldName));
        }

        public static IStitchingBuilder AddMergeHandler<T>(
           this IStitchingBuilder builder)
           where T : ITypeMergeHanlder
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddMergeHandler(SchemaMergerExtensions.CreateHandler<T>());

            return builder;
        }
    }
}