﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HavenSoft.HexManiac.Core.ViewModels.Map {
   public abstract class MapSlider : ViewModelCore {
      public const int SliderSize = 20;
      protected readonly int id;

      private MapSliderIcons icon;
      public MapSliderIcons Icon { get => icon; set => SetEnum(ref icon, value); }

      public bool EnableContextMenu => ContextItems.Count > 0;
      public ObservableCollection<SliderCommand> ContextItems { get; } = new();

      private bool anchorLeftEdge, anchorTopEdge;
      private int anchorX, anchorY;
      public bool AnchorLeftEdge { get => anchorLeftEdge; set => Set(ref anchorLeftEdge, value); } // if false, we anchor the right edge instead
      public bool AnchorTopEdge { get => anchorTopEdge; set => Set(ref anchorTopEdge, value); }    // if false, we anchor the bottom edge instead
      public int AnchorPositionX { get => anchorX; set => Set(ref anchorX, value); }
      public int AnchorPositionY { get => anchorY; set => Set(ref anchorY, value); }

      public MapSlider(int id, MapSliderIcons icon, int left = int.MinValue, int top = int.MinValue, int right = int.MinValue, int bottom = int.MinValue) {
         AnchorPositionX = left;
         AnchorLeftEdge = AnchorPositionX != int.MinValue;
         if (!AnchorLeftEdge) AnchorPositionX = -right;
         AnchorPositionY = top;
         AnchorTopEdge = AnchorPositionY != int.MinValue;
         if (!AnchorTopEdge) AnchorPositionY = -bottom;
         Icon = icon;
         this.id = id;
      }

      public abstract void Move(int x, int y);

      public virtual bool TryUpdate(MapSlider? other) {
         if (other.id != id) return false;
         AnchorLeftEdge = other.AnchorLeftEdge;
         AnchorTopEdge = other.AnchorTopEdge;
         AnchorPositionX = other.AnchorPositionX;
         AnchorPositionY = other.AnchorPositionY;
         Icon = other.Icon;
         return true;
      }
   }

   public enum MapSliderIcons {
      None, LeftRight, UpDown, X
   }

   public class ConnectionSlider : MapSlider {
      private readonly Action notify;
      private readonly ConnectionModel connection;

      public ConnectionSlider(ConnectionModel connection, Action notify, int id, MapSliderIcons icon, int left = int.MinValue, int top = int.MinValue, int right = int.MinValue, int bottom = int.MinValue) : base(id, icon, left, top, right, bottom) {
         (this.notify, this.connection) = (notify, connection);
      }

      public override void Move(int x, int y) {
         if (Icon == MapSliderIcons.LeftRight) {
            connection.Offset += x;
            if (x != 0) notify();
         } else if (Icon == MapSliderIcons.UpDown) {
            connection.Offset += y;
            if (y != 0) notify();
         } else {
            throw new NotImplementedException();
         }
      }

      public override bool TryUpdate(MapSlider? that) {
         if (that is not ConnectionSlider other) return false;
         if (other.connection.MapNum != connection.MapNum ||
            other.connection.MapGroup != connection.MapGroup) return false;
         if (!base.TryUpdate(other)) return false;
         return true;
      }
   }

   public class ExpansionSlider : MapSlider {
      private Action<MapDirection, int> resize;

      public ExpansionSlider(Action<MapDirection, int> resize, int id, MapSliderIcons icon, IEnumerable<SliderCommand> contextItems, int left = int.MinValue, int top = int.MinValue, int right = int.MinValue, int bottom = int.MinValue) : base(id, icon, left, top, right, bottom) {
         foreach (var item in contextItems) ContextItems.Add(item);
         this.resize = resize;
      }

      public override void Move(int x, int y) {
         if (Icon == MapSliderIcons.LeftRight && !AnchorLeftEdge) {
            resize(MapDirection.Left, -x);
         } else if (Icon == MapSliderIcons.LeftRight && AnchorLeftEdge) {
            resize(MapDirection.Right, x);
         } else if (Icon == MapSliderIcons.UpDown && !AnchorTopEdge) {
            resize(MapDirection.Up, -y);
         } else if (Icon == MapSliderIcons.UpDown && AnchorTopEdge) {
            resize(MapDirection.Down, y);
         }
      }

      public override bool TryUpdate(MapSlider? that) {
         if (that is not ExpansionSlider other) return false;
         if (!base.TryUpdate(other)) return false;
         resize = other.resize;
         return true;
      }
   }

   public class SliderCommand : ViewModelCore, ICommand {
      private readonly Action<object> action;
      public event EventHandler? CanExecuteChanged;
      public string Text { get; }
      public object Parameter { get; init; }

      public SliderCommand(string text, Action<object> action) => (Text, this.action) = (text, action);

      public bool CanExecute(object? parameter) => true;

      public void Execute(object? parameter) => action(parameter);
   }
}
