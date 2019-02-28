using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Lot of logic copied from https://github.com/aspnet/AspNetCore/blob/master/src/Http/Http.Abstractions/src/IMiddleware.cs
/// </summary>
namespace DeviceReader.Services
{
    
    public delegate Task TransformDelegate<Tin>(Tin context);
    
    public interface ITransformBuilder<Tin>
    {
        TransformDelegate<Tin> Build();
        ITransformBuilder<Tin> Use(Func<TransformDelegate<Tin>, TransformDelegate<Tin>> transform);
    }
}
