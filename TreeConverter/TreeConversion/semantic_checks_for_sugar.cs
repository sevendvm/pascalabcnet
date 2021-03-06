﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PascalABCCompiler.SyntaxTree;
using PascalABCCompiler.TreeRealization;

namespace PascalABCCompiler.TreeConverter
{
    public partial class syntax_tree_visitor
    {
        public void semantic_check_assign_tuple(addressed_value_list vars, expression ex) 
        {
            // Проверить, что справа - Tuple
            var expr = convert_strong(ex);
            expr = convert_if_typed_expression_to_function_call(expr);

            var t = ConvertSemanticTypeNodeToNETType(expr.type);
            if (t == null)
                AddError(expr.location, "TUPLE_EXPECTED");

            if (!t.FullName.StartsWith("System.Tuple"))
                AddError(expr.location, "TUPLE_EXPECTED");

            var n = vars.variables.Count();
            if (n > t.GetGenericArguments().Count())
                AddError(get_location(vars), "TOO_MANY_ELEMENTS_ON_LEFT_SIDE_OF_TUPLE_ASSIGNMRNT");
        }

        public void semantic_check_assign_var_tuple(ident_list vars, expression ex) 
        {
            // Проверить, что справа - Tuple
            var expr = convert_strong(ex);
            expr = convert_if_typed_expression_to_function_call(expr);

            var t = ConvertSemanticTypeNodeToNETType(expr.type);
            if (t == null)
                AddError(expr.location, "TUPLE_EXPECTED");

            if (!t.FullName.StartsWith("System.Tuple"))
                AddError(expr.location, "TUPLE_EXPECTED");

            var n = vars.idents.Count();
            if (n > t.GetGenericArguments().Count())
                AddError(get_location(vars), "TOO_MANY_ELEMENTS_ON_LEFT_SIDE_OF_TUPLE_ASSIGNMRNT");
        }

        void semantic_check_method_call_as_slice_expr(SyntaxTree.method_call mc)
        // нельзя проверять сахарный узел, т.к.могут быть вложенные сахарные expression!!
        {
            var v = (mc.dereferencing_value as dot_node).left;
            var from = mc.parameters.expressions[1];
            var to = mc.parameters.expressions[2];
            expression step = mc.parameters.expressions.Count > 3 ? mc.parameters.expressions[3] : null;

            var semvar = convert_strong(v);
            if (semvar is typed_expression)
                semvar = convert_typed_expression_to_function_call(semvar as typed_expression);

            var IsSlicedType = 0; // проверим, является ли semvar.type динамическим массивом, списком List или строкой
            if (semvar.type.type_special_kind == SemanticTree.type_special_kind.array_kind)
                IsSlicedType = 1;

            if (IsSlicedType == 0)
            {
                var t = ConvertSemanticTypeNodeToNETType(semvar.type); // не работает для array of T

                // semvar.type должен быть array of T, List<T> или string
                if (t == null)
                    IsSlicedType = 0; // можно ничего не присваивать :)
//                else if (t.IsArray)
//                  IsSlicedType = 1;
                else if (t == typeof(System.String))
                    IsSlicedType = 2;
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                    IsSlicedType = 3;
            }

            if (IsSlicedType == 0)
                AddError(get_location(v), "BAD_SLICE_OBJECT");

            var semfrom = convert_strong(from);
            var b = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.integer_type);
            if (!b)
                AddError(get_location(from), "INTEGER_VALUE_EXPECTED");

            var semto = convert_strong(to);
            b = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.integer_type);
            if (!b)
                AddError(get_location(to), "INTEGER_VALUE_EXPECTED");

            if (step != null)
            {
                var semstep = convert_strong(step);
                b = convertion_data_and_alghoritms.can_convert_type(semstep, SystemLibrary.SystemLibrary.integer_type);
                if (!b)
                    AddError(get_location(step), "INTEGER_VALUE_EXPECTED");
            }

        }
        /*void semantic_check_tuple(SyntaxTree.tuple_node tup)
        {
            //if (tup.el.expressions.Count > 7) 
			//	AddError(get_location(tup),"TUPLE_ELEMENTS_COUNT_MUST_BE_LESSEQUAL_7");
        }*/

        public void semantic_check_dot_question(SyntaxTree.question_colon_expression qce)
        {
            var av = convert_strong((qce.condition as bin_expr).left);
            Type t = null;
            if (av.type is compiled_generic_instance_type_node ctn2)
                t = ctn2.compiled_original_generic.compiled_type;
            else if (av.type is compiled_type_node ctn1)
                t = ctn1.compiled_type;

            if (!av.type.is_class && !(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)))
                AddError(av.location, "OPERATOR_DQ_MUST_BE_USED_WITH_A_REFERENCE_TYPE_VALUETYPE");
        }

        public void semantic_check_loop_stmt(SyntaxTree.expression ex)
        {
            var sem_ex = convert_strong(ex);
            var b = convertion_data_and_alghoritms.can_convert_type(sem_ex, SystemLibrary.SystemLibrary.integer_type);
            if (!b)
                AddError(sem_ex.location, "INTEGER_VALUE_EXPECTED");
        }


        /*void common_diap_check(expression from, expression to)
        {

        }*/

        void semantic_check_method_call_as_diapason_expr(SyntaxTree.method_call mc)
        {
            var from = mc.parameters.expressions[0];
            var to = mc.parameters.expressions[1];

            var semfrom = convert_strong(from);
            var b = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.integer_type);
            var b1 = false;
            if (!b)
                b1 = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.char_type);
            if (!b && !b1)
                AddError(get_location(from), "INTEGER_OR_CHAR_VALUE_EXPECTED");

            var semto = convert_strong(to);
            var c = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.integer_type);
            var c1 = false;
            if (!c)
                c1 = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.char_type);
            if (!c && !c1)
                AddError(get_location(to), "INTEGER_OR_CHAR_VALUE_EXPECTED");
            if (b != c || c1 != b1)
                AddError(get_location(to), "INCOMPATIBLE_DIAPASON_BOUNDS_TYPES");
        }

        void semantic_check_method_call_as_inrange_expr(SyntaxTree.method_call mc)
        {
            var v = mc.parameters.expressions[0];
            var from = mc.parameters.expressions[1];
            var to = mc.parameters.expressions[2];

            var semv = convert_strong(v);
            var a = convertion_data_and_alghoritms.can_convert_type(semv, SystemLibrary.SystemLibrary.integer_type);
            var a1 = false;
            if (!a)
                a1 = convertion_data_and_alghoritms.can_convert_type(semv, SystemLibrary.SystemLibrary.char_type);
            if (!a && !a1)
                AddError(get_location(v), "INTEGER_OR_CHAR_VALUE_EXPECTED");

            var semfrom = convert_strong(from);
            var b = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.integer_type);
            var b1 = false;
            if (!b)
                b1 = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.char_type);
            if (!b && !b1)
                AddError(get_location(from), "INTEGER_OR_CHAR_VALUE_EXPECTED");

            var semto = convert_strong(to);
            var c = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.integer_type);
            var c1 = false;
            if (!c)
                c1 = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.char_type);
            if (!c && !c1)
                AddError(get_location(to), "INTEGER_OR_CHAR_VALUE_EXPECTED");

            if (b != c || c1 != b1)
                AddError(get_location(to), "INCOMPATIBLE_DIAPASON_BOUNDS_TYPES");
            if (a != b || a1 != b1)
                AddError(get_location(v), "INCOMPATIBLE_TYPES_OF_ELEMENT_AND_DIAPASON");
        }

        void semantic_check_for_new_range(SyntaxTree.diapason_expr_new diap, type_definition td, ident id)
        {
            var from = diap.left;
            var to = diap.right;

            var semfrom = convert_strong(from);
            var b = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.integer_type);
            var b1 = false;
            if (!b)
                b1 = convertion_data_and_alghoritms.can_convert_type(semfrom, SystemLibrary.SystemLibrary.char_type);
            if (!b && !b1)
                AddError(get_location(from), "INTEGER_OR_CHAR_VALUE_EXPECTED");

            var semto = convert_strong(to);
            var c = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.integer_type);
            var c1 = false;
            if (!c)
                c1 = convertion_data_and_alghoritms.can_convert_type(semto, SystemLibrary.SystemLibrary.char_type);
            if (!c && !c1)
                AddError(get_location(to), "INTEGER_OR_CHAR_VALUE_EXPECTED");

            if (b != c || c1 != b1)
                AddError(get_location(to), "INCOMPATIBLE_DIAPASON_BOUNDS_TYPES");

            if (td != null && !(td is no_type_foreach))
            {
                // то мы определили тип явно в заголовке
                var semtype = convert_strong(td);
                var d = convertion_data_and_alghoritms.can_convert_type(semtype, SystemLibrary.SystemLibrary.integer_type);
                var d1 = false;
                if (!d)
                    d1 = convertion_data_and_alghoritms.can_convert_type(semtype, SystemLibrary.SystemLibrary.char_type);
                if (!d && !d1)
                    AddError(get_location(td), "INTEGER_OR_CHAR_VALUE_EXPECTED");
                if (b != d || b1 != d1)
                    AddError(get_location(id), "INCOMPATIBLE_TYPES_OF_ELEMENT_AND_DIAPASON");
            }
            else if (td == null)
            {
                var semid = convert_strong(id);
                var e = convertion_data_and_alghoritms.can_convert_type(semid, SystemLibrary.SystemLibrary.integer_type);
                var e1 = false;
                if (!e)
                    e1 = convertion_data_and_alghoritms.can_convert_type(semid, SystemLibrary.SystemLibrary.char_type);
                if (!e && !e1)
                    AddError(get_location(id), "INTEGER_OR_CHAR_VALUE_EXPECTED");

                if (b != e || b1 != e1)
                    AddError(get_location(id), "INCOMPATIBLE_TYPES_OF_ELEMENT_AND_DIAPASON");
            }
        }

        void semantic_check_for_indices(SyntaxTree.expression expr)
        {
            // Надо проверить, что expr - это одноразмерный массив или список. Эта проверка была в visit(foreach)
            var semexpr = convert_strong(expr);
            var Is1DArr = Is1DArray(semexpr);
            var il = IsIList(semexpr);
            if (!Is1DArr && !il)
                AddError(get_location(expr), "ONE_DIM_ARRAY_OR_LIST_EXPECTED");
        }


    }
}
