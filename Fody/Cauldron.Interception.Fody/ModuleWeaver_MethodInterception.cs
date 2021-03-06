﻿using Cauldron.Interception.Cecilator;
using Cauldron.Interception.Fody.Extensions;
using Cauldron.Interception.Fody.HelperTypes;
using System.Collections.Generic;
using System.Linq;

namespace Cauldron.Interception.Fody
{
    public sealed partial class ModuleWeaver
    {
        private void ImplementTypeWideMethodInterception(Builder builder, IEnumerable<BuilderType> attributes)
        {
            if (!attributes.Any())
                return;

            using (new StopwatchLog(this, "class wide method"))
            {
                var types = builder
                    .FindTypesByAttributes(attributes)
                    .GroupBy(x => x.Type)
                    .Select(x => new
                    {
                        x.Key,
                        Item = x.ToArray()
                    })
                    .ToArray();

                foreach (var type in types)
                {
                    this.Log($"Implementing interceptors in type {type.Key.Fullname}");

                    foreach (var method in type.Key.Methods)
                    {
                        if (method.IsConstructor || method.IsPropertyGetterSetter)
                            continue;

                        for (int i = 0; i < type.Item.Length; i++)
                            method.CustomAttributes.Copy(type.Item[i].Attribute);
                    }

                    for (int i = 0; i < type.Item.Length; i++)
                        type.Item[i].Remove();
                }
            }
        }

        private void InterceptMethods(Builder builder, IEnumerable<BuilderType> attributes)
        {
            if (!attributes.Any())
                return;

            using (new StopwatchLog(this, "method"))
            {
                var asyncTaskMethodBuilder = new __AsyncTaskMethodBuilder();
                var asyncTaskMethodBuilderGeneric = new __AsyncTaskMethodBuilder_1();
                var syncRoot = new __ISyncRoot();
                var task = new __Task();
                var exception = new __Exception();

                var methods = builder
                    .FindMethodsByAttributes(attributes)
                    .Where(x => !x.Method.IsPropertyGetterSetter)
                    .GroupBy(x => new MethodKey(x.Method, x.AsyncMethod))
                    .Select(x => new MethodBuilderInfo<MethodBuilderInfoItem<__IMethodInterceptor>>(x.Key, x.Select(y => new MethodBuilderInfoItem<__IMethodInterceptor>(y, __IMethodInterceptor.Instance))))
                    .OrderBy(x => x.Key.Method.DeclaringType.Fullname)
                    .ToArray();

                foreach (var method in methods)
                {
                    if (method.Item == null || method.Item.Length == 0)
                        continue;

                    this.Log($"Implementing method interceptors: {method.Key.Method.DeclaringType.Name.PadRight(40, ' ')} {method.Key.Method.Name}({string.Join(", ", method.Key.Method.Parameters.Select(x => x.Name))})");

                    var targetedMethod = method.Key.AsyncMethod ?? method.Key.Method;
                    var attributedMethod = method.Key.Method;

                    var typeInstance = method.Key.Method.AsyncMethodHelper.Instance;
                    var interceptorField = new Field[method.Item.Length];

                    if (method.RequiresSyncRootField)
                    {
                        if (method.SyncRoot.IsStatic)
                            targetedMethod.AsyncOriginType.CreateStaticConstructor().NewCode()
                                .Assign(method.SyncRoot).NewObj(builder.GetType(typeof(object)).Import().ParameterlessContructor)
                                .Insert(InsertionPosition.Beginning);
                        else
                            foreach (var ctors in targetedMethod.AsyncOriginType.GetRelevantConstructors().Where(x => x.Name == ".ctor"))
                                ctors.NewCode().Assign(method.SyncRoot).NewObj(builder.GetType(typeof(object)).Import().ParameterlessContructor).Insert(InsertionPosition.Beginning);
                    }

                    var code = targetedMethod
                    .NewCode()
                        .Context(x =>
                        {
                            for (int i = 0; i < method.Item.Length; i++)
                            {
                                var item = method.Item[i];
                                var name = $"<{targetedMethod.Name}>_attrib{i}_{item.Attribute.Identification}";
                                interceptorField[i] = targetedMethod.AsyncOriginType.CreateField(targetedMethod.Modifiers.GetPrivate(), item.Interface.ToBuilderType, name);
                                interceptorField[i].CustomAttributes.AddNonSerializedAttribute();

                                x.Load(interceptorField[i]).IsNull().Then(y =>
                                {
                                    y.Assign(interceptorField[i]).NewObj(item.Attribute);
                                    if (item.HasSyncRootInterface)
                                        y.Load(interceptorField[i]).As(__ISyncRoot.Type).Call(syncRoot.SyncRoot, method.SyncRoot);

                                    ImplementAssignMethodAttribute(builder, method.Item[i].AssignMethodAttributeInfos, interceptorField[i], item.Attribute.Attribute.Type, x);
                                });
                                item.Attribute.Remove();
                            }
                        })
                        .Try(x =>
                        {
                            for (int i = 0; i < method.Item.Length; i++)
                            {
                                var item = method.Item[i];
                                x.Load(interceptorField[i]).Call(item.Interface.OnEnter, attributedMethod.OriginType, typeInstance, attributedMethod, x.GetParametersArray());
                            }

                            x.OriginalBody();
                        });

                    if (method.Key.AsyncMethod == null)
                        code.Catch(__Exception.Type, x =>
                        {
                            x.Or(method.Item, (coder, y, i) => coder.Load(interceptorField[i]).Call(y.Interface.OnException, x.Exception));
                            x.IsTrue().Then(y => x.Rethrow());
                            x.ReturnDefault();
                        });

                    code.Finally(x =>
                    {
                        for (int i = 0; i < method.Item.Length; i++)
                            x.Load(interceptorField[i]).Call(method.Item[i].Interface.OnExit);
                    })
                      .EndTry()
                      .Return()
                  .Replace();

                    if (method.Key.AsyncMethod != null)
                    {
                        // Special case for async methods
                        targetedMethod
                            .NewCode().Context(x =>
                            {
                                var exceptionVariable = method.Key.Method.AsyncMethodHelper.GetAsyncStateMachineExceptionVariable();
                                var exceptionBlock = method.Key.Method.AsyncMethodHelper.GetAsyncStateMachineExceptionBlock();

                                for (int i = 0; i < method.Item.Length; i++)
                                {
                                    x.Load(interceptorField[i]).Call(method.Item[i].Interface.OnException, exceptionVariable);

                                    if (method.Item.Length - 1 < i)
                                        x.Or();
                                }

                                x.IsFalse().Then(y => y.Leave(exceptionBlock.End)).Insert(InsertionAction.After, exceptionBlock.Start);
                            });
                    }
                };
            }
        }
    }
}