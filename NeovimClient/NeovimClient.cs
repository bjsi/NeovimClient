﻿using MsgPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NeovimClient {

    public static class NeovimExt {
        public static long AsInt64orExt( this MessagePackObject o ) {
            if( (bool)o.IsTypeOf<long>() ) {
                return o.AsInt64();
            }

            var bytes = o.AsMessagePackExtendedTypeObject().GetBody();
            if ( bytes.Length < 8 ) {
                Array.Resize( ref bytes, 8 );
            }

            if ( !BitConverter.IsLittleEndian ) {
                Array.Reverse( bytes );
            }

            return BitConverter.ToInt64( bytes, 0 );
        }
    }

    public class NeovimFuncInfo {
        private List<Type>   paramTypeCs     = new List<Type>();
        private List<string> paramTypeNeovim = new List<string>();

        public string Name { get; private set; }
        public string Description { get; private set; }
        public ReadOnlyCollection<Type> ParamTypeCs { get { return new ReadOnlyCollection<Type>( paramTypeCs ); } }
        public ReadOnlyCollection<string> ParamTypeNeovim { get { return new ReadOnlyCollection<string>( paramTypeNeovim ); } }
        public Type ReturnTypeCs { get; private set; }
        public string ReturnTypeNeovim { get; private set; }
        public bool IsAsync { get; private set; }
        public bool CanFail { get; private set; }
        public object Func { get; private set; }

        public NeovimFuncInfo( string name, string dscr, List<string> paramType, string returnType, object func, bool isAsync, bool canFail ) {
            Name = name;
            Description = dscr;
            ReturnTypeNeovim = returnType;
            ReturnTypeCs = NeovimUtil.ConvTypeNeovimToCs( returnType );
            IsAsync = isAsync;
            CanFail = canFail;
            Func = func;

            foreach ( var p in paramType ) {
                paramTypeNeovim.Add( p );
                paramTypeCs.Add( NeovimUtil.ConvTypeNeovimToCs( p ) );
            }
        }
    }

    public class NeovimClient<T> : IDisposable where T : INeovimIO {

        // - field -----------------------------------------------------------------------

        private T neovimIO;
        private Dictionary<string, NeovimFuncInfo> api = new Dictionary<string, NeovimFuncInfo>();

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimClient( T io ) {
            neovimIO = io;
        }

        public void Init() {
            neovimIO.Init();
            CreateApi();
        }

        public void Dispose() {
            neovimIO.Dispose();
        }

        public void Action                        ( string name                                           ) { ( api[name].Func as Action                         )(                        ); }
        public void Action<T1                    >( string name, T1 p0                                    ) { ( api[name].Func as Action<T1                    > )( p0                     ); }
        public void Action<T1, T2                >( string name, T1 p0, T2 p1                             ) { ( api[name].Func as Action<T1, T2                > )( p0, p1                 ); }
        public void Action<T1, T2, T3            >( string name, T1 p0, T2 p1, T3 p2                      ) { ( api[name].Func as Action<T1, T2, T3            > )( p0, p1, p2             ); }
        public void Action<T1, T2, T3, T4        >( string name, T1 p0, T2 p1, T3 p2, T4 p3               ) { ( api[name].Func as Action<T1, T2, T3, T4        > )( p0, p1, p2, p3         ); }
        public void Action<T1, T2, T3, T4, T5    >( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4        ) { ( api[name].Func as Action<T1, T2, T3, T4, T5    > )( p0, p1, p2, p3, p4     ); }
        public void Action<T1, T2, T3, T4, T5, T6>( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5 ) { ( api[name].Func as Action<T1, T2, T3, T4, T5, T6> )( p0, p1, p2, p3, p4, p5 ); }

        public T1 Func<T1                        >( string name                                           ) { return ( api[name].Func as Func<T1                        > )(                        ); }
        public T2 Func<T1, T2                    >( string name, T1 p0                                    ) { return ( api[name].Func as Func<T1, T2                    > )( p0                     ); }
        public T3 Func<T1, T2, T3                >( string name, T1 p0, T2 p1                             ) { return ( api[name].Func as Func<T1, T2, T3                > )( p0, p1                 ); }
        public T4 Func<T1, T2, T3, T4            >( string name, T1 p0, T2 p1, T3 p2                      ) { return ( api[name].Func as Func<T1, T2, T3, T4            > )( p0, p1, p2             ); }
        public T5 Func<T1, T2, T3, T4, T5        >( string name, T1 p0, T2 p1, T3 p2, T4 p3               ) { return ( api[name].Func as Func<T1, T2, T3, T4, T5        > )( p0, p1, p2, p3         ); }
        public T6 Func<T1, T2, T3, T4, T5, T6    >( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4        ) { return ( api[name].Func as Func<T1, T2, T3, T4, T5, T6    > )( p0, p1, p2, p3, p4     ); }
        public T7 Func<T1, T2, T3, T4, T5, T6, T7>( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5 ) { return ( api[name].Func as Func<T1, T2, T3, T4, T5, T6, T7> )( p0, p1, p2, p3, p4, p5 ); }

        public event NeovimNotificationEventHandler NotificationReceived {
            add    { neovimIO.NotificationReceived += value; }
            remove { neovimIO.NotificationReceived -= value; }
        }

        public ReadOnlyCollection<string> GetFuncs() {
            return new ReadOnlyCollection<string>( api.Keys.ToList() );
        }

        public NeovimFuncInfo GetFuncInfo( string name ) {
            return api[name];
        }

        // - private methods -------------------------------------------------------------

        public MessagePackObject GetApiInfo() {
            return neovimIO.Request( "vim_get_api_info", new object[] {} )[1];
        }

        private void CreateApi() {
            api.Clear();
            var funcs = GetApiInfo().AsList()[1].AsDictionary()["functions"].AsList();
            var funcsStatic = GetApiStatic();

            foreach( var f in funcs.Concat( funcsStatic ) ) {
                var func = f.AsDictionary();

                var parameters = func["parameters"].AsList();
                var async      = ( func.ContainsKey( "async" )    ) ? func["async"].AsBoolean() : false;
                var canFail    = ( func.ContainsKey( "can_fail" ) ) ? func["can_fail"].AsBoolean() : false;
                var returnType = func["return_type"].AsString();
                var name       = func["name"].AsString();

                var param = new List<string>();

                var dscr0 = ( NeovimUtil.ConvTypeNeovimToCs( returnType ) == typeof( void ) ) ? "void Action<" : NeovimUtil.ConvTypeNeovimToCs( returnType ).Name + " Func<";
                var dscr1 = "( String name, ";
                foreach( var p in parameters ) {
                    var pType = p.AsList()[0].AsString();
                    var pName = p.AsList()[1].AsString();
                    param.Add( pType );
                    dscr0 += NeovimUtil.ConvTypeNeovimToCs( pType ).Name + ", ";
                    dscr1 += NeovimUtil.ConvTypeNeovimToCs( pType ).Name + " " + pName + ", ";
                }
                dscr0 = dscr0.Remove( dscr0.Length - 2 );
                dscr1 = dscr1.Remove( dscr1.Length - 2 );
                var dscr = dscr0 + ">" + dscr1 + " )";

                var expr = CreateExpression( name, returnType, param, async, canFail );
                var neovimFunc = new NeovimFuncInfo( name, dscr, param, returnType, expr, async, canFail );
                if (!api.TryGetValue(name, out _))
                    api.Add( name, neovimFunc );
            }
        }

        private List<MessagePackObject> GetApiStatic() {
            var ui_attach_dict = new Dictionary<MessagePackObject, MessagePackObject>();
            var ui_attach_param  = new List<MessagePackObject>();
            ui_attach_param.Add( new MessagePackObject( new List<MessagePackObject>() { "Integer", "width" } ) );
            ui_attach_param.Add( new MessagePackObject( new List<MessagePackObject>() { "Integer", "height" } ) );
            ui_attach_param.Add( new MessagePackObject( new List<MessagePackObject>() { "Boolean", "rgb" } ) );
            ui_attach_dict["name"] = "ui_attach";
            ui_attach_dict["parameters"] = new MessagePackObject( ui_attach_param );
            ui_attach_dict["async"] = false;
            ui_attach_dict["can_fail"] = false;
            ui_attach_dict["return_type"] = "void";

            var ui_detach_dict = new Dictionary<MessagePackObject, MessagePackObject>();
            var ui_detach_param  = new List<MessagePackObject>();
            ui_detach_dict["name"] = "ui_detach";
            ui_detach_dict["parameters"] = new MessagePackObject( ui_detach_param );
            ui_detach_dict["async"] = false;
            ui_detach_dict["can_fail"] = false;
            ui_detach_dict["return_type"] = "void";

            var ui_try_resize_dict = new Dictionary<MessagePackObject, MessagePackObject>();
            var ui_try_resize_param  = new List<MessagePackObject>();
            ui_try_resize_param.Add( new MessagePackObject( new List<MessagePackObject>() { "Integer", "width" } ) );
            ui_try_resize_param.Add( new MessagePackObject( new List<MessagePackObject>() { "Integer", "height" } ) );
            ui_try_resize_dict["name"] = "ui_try_resize";
            ui_try_resize_dict["parameters"] = new MessagePackObject( ui_try_resize_param );
            ui_try_resize_dict["async"] = false;
            ui_try_resize_dict["can_fail"] = false;
            ui_try_resize_dict["return_type"] = "void";

            var ret = new List<MessagePackObject>();
            ret.Add( new MessagePackObject( new MessagePackObjectDictionary( ui_attach_dict ) ) );
            ret.Add( new MessagePackObject( new MessagePackObjectDictionary( ui_detach_dict ) ) );
            ret.Add( new MessagePackObject( new MessagePackObjectDictionary( ui_try_resize_dict ) ) );

            return ret;
        }

        private object CreateExpression( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( param.Count > 6 ) {
                throw new NotImplementedException();
            }

            var isAction = NeovimUtil.ConvTypeNeovimToCs( returnType ) == typeof( void );
            if (  isAction && param.Count == 0 ) {
                return CreateAction( name, returnType, param, async, canFail );
            } else {
                if( !isAction ) {
                    param.Add( returnType );
                }
                var creatorName = ( isAction ) ? "CreateAction" : "CreateFunc";
                var methods = typeof( NeovimClient<T> ).GetMethods( BindingFlags.NonPublic | BindingFlags.Instance );
                var baseMethod = methods.First( m => m.Name == creatorName && m.GetGenericArguments().Count() == param.Count );
                var method = baseMethod.MakeGenericMethod( param.Select( i => NeovimUtil.ConvTypeNeovimToCs( i ) ).ToArray() );
                return method.Invoke( this, new object[] { name, returnType, param, async, canFail } );
            }
        }

        private object CreateFunc<T1>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T1 ) == typeof( long              ) ) { Expression<Func<long             >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T1 ) == typeof( long[]            ) ) { Expression<Func<long[]           >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T1 ) == typeof( string            ) ) { Expression<Func<string           >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T1 ) == typeof( string[]          ) ) { Expression<Func<string[]         >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T1 ) == typeof( bool              ) ) { Expression<Func<bool             >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T1 ) == typeof( object            ) ) { Expression<Func<object           >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T1 ) == typeof( MessagePackObject ) ) { Expression<Func<MessagePackObject>> func = ( () => neovimIO.Request( name, new object[] {}, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T2 ) == typeof( long              ) ) { Expression<Func<T1, long             >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T2 ) == typeof( long[]            ) ) { Expression<Func<T1, long[]           >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T2 ) == typeof( string            ) ) { Expression<Func<T1, string           >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T2 ) == typeof( string[]          ) ) { Expression<Func<T1, string[]         >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T2 ) == typeof( bool              ) ) { Expression<Func<T1, bool             >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T2 ) == typeof( object            ) ) { Expression<Func<T1, object           >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T2 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, MessagePackObject>> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T3 ) == typeof( long              ) ) { Expression<Func<T1, T2, long             >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T3 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, long[]           >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T3 ) == typeof( string            ) ) { Expression<Func<T1, T2, string           >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T3 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, string[]         >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T3 ) == typeof( bool              ) ) { Expression<Func<T1, T2, bool             >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T3 ) == typeof( object            ) ) { Expression<Func<T1, T2, object           >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T3 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, MessagePackObject>> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T4 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, long             >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T4 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, long[]           >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T4 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, string           >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T4 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, string[]         >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T4 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, bool             >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T4 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, object           >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T4 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, MessagePackObject>> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4, T5>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T5 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, T4, long             >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T5 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, T4, long[]           >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T5 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, T4, string           >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T5 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, T4, string[]         >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T5 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, T4, bool             >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T5 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, T4, object           >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T5 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, T4, MessagePackObject>> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4, T5, T6>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T6 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, T4, T5, long             >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T6 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, T4, T5, long[]           >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T6 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, T4, T5, string           >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T6 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, T4, T5, string[]         >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T6 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, T4, T5, bool             >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T6 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, T4, T5, object           >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T6 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, T4, T5, MessagePackObject>> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4, T5, T6, T7>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T7 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, long             >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsInt64() );                                      return func.Compile(); }
            if( typeof( T7 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, long[]           >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsList().Select( i => i.AsInt64() ).ToArray() );  return func.Compile(); }
            if( typeof( T7 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, string           >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsString() );                                     return func.Compile(); }
            if( typeof( T7 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, string[]         >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() ); return func.Compile(); }
            if( typeof( T7 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, bool             >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsBoolean() );                                    return func.Compile(); }
            if( typeof( T7 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, object           >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].ToObject() );                                     return func.Compile(); }
            if( typeof( T7 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, MessagePackObject>> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1] );                                                return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateAction( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action> func = ( () => neovimIO.Request( name, new object[] {}, true ) );
            return func.Compile();
        }

        private object CreateAction<T1>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1>> func = ( p => neovimIO.Request( name, new object[] { p }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2>> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3>> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4>> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4, T5>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4, T5>> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4, T5, T6>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4, T5, T6>> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4, T5, T6, T7>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4, T5, T6, T7>> func = ( ( p0, p1, p2, p3, p4, p5, p6 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5, p6 }, true ) );
            return func.Compile();
        }

    }

    class NeovimUtil {
        public static Type ConvTypeNeovimToCs( string type ) {
            if ( type == "Buffer" || type == "Window" || type == "Tabpage" ) {
                return typeof( long );
            } else if ( type.StartsWith( "ArrayOf(Integer" ) ) {
                return typeof( long[] );
            } else if ( type.StartsWith( "ArrayOf(Buffer" ) || type.StartsWith( "ArrayOf(Window" ) || type.StartsWith( "ArrayOf(Tabpage" ) ) {
                return typeof( long[] );
            } else if ( type.StartsWith( "ArrayOf(String" ) ) {
                return typeof( string[] );
            } else {
                switch ( type ) {
                    case "Integer"   : return typeof( long );
                    case "String"    : return typeof( string );
                    case "void"      : return typeof( void );
                    case "Boolean"   : return typeof( bool );
                    case "Object"    : return typeof( object );
                    case "Array"     : return typeof( MessagePackObject );
                    case "Dictionary": return typeof( MessagePackObject );
                    default          : return typeof( MessagePackObject );
                }
            }
        }
    }

}
