using System;
using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Menu;
using BelowTheWing.Net;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.EditorTools
{
    public static class SerializedFields
    {
        public static void SetList(UnityEngine.Object target, string field, IReadOnlyList<UnityEngine.Object> values)
        {
            var serialized = new SerializedObject(target);
            var property = Field(serialized, target, field);

            property.arraySize = values.Count;

            for (var i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);

            Field(serialized, target, field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);

            Field(serialized, target, field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Field(SerializedObject serialized, UnityEngine.Object target, string field)
            => serialized.FindProperty(field)
               ?? throw new InvalidOperationException(
                   $"{target.GetType().Name} has no serialized field called '{field}', so the scene " +
                   "being built would carry an empty reference where that value was meant to go.");
    }
}
