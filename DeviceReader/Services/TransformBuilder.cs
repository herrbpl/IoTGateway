using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Services
{
    /// <summary>
    /// https://github.com/aspnet/AspNetCore/blob/master/src/Http/Http/src/Internal/ApplicationBuilder.cs
    /// </summary>
    /// <typeparam name="Tin"></typeparam>    
    public class TransformBuilder<Tin> : ITransformBuilder<Tin>
    {

        private readonly IList<Func<TransformDelegate<Tin>, TransformDelegate<Tin>>> _components = new List<Func<TransformDelegate<Tin>, TransformDelegate<Tin>>>();

        public TransformDelegate<Tin> Build()
        {

            TransformDelegate<Tin> transform = (input) =>
             {
                 return Task.FromResult(input);
             };

            foreach (var component in _components.Reverse())
            {
                transform = component(transform);
            }
            return transform;
        }

        public ITransformBuilder<Tin> Use(Func<TransformDelegate<Tin>, TransformDelegate<Tin>> transform)
        {
            _components.Add(transform);
            return this;            
        }
    }
    public static class TransformExtensions
    {
        public static ITransformBuilder<T> Use<T>(this ITransformBuilder<T> builder, Func<T, Func<Task>, Task > transformer)
        {
            return builder.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return transformer(context, simpleNext);
                };
            });
        }
    }
}
