using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CocosSharp;

namespace tests
{
    public class SchedulerTestLayer : CCLayer
    {
        public override void OnEnter()
        {
            base.OnEnter();

            CCSize s = Layer.VisibleBoundsWorldspace.Size;

            var label = new CCLabel(title(), "arial", 32, CCLabelFormat.SpriteFont);
            AddChild(label);
            label.Position = (new CCPoint(s.Width / 2, s.Height - 50));

            string subTitle = subtitle();
            if (!string.IsNullOrEmpty(subTitle))
            {
                var l = new CCLabel(subTitle, "arial", 16, CCLabelFormat.SpriteFont);
                AddChild(l, 1);
                l.Position = new CCPoint(s.Width / 2, s.Height - 80);
            }

            CCMenuItemImage item1 = new CCMenuItemImage("Images/b1", "Images/b2", backCallback);
            CCMenuItemImage item2 = new CCMenuItemImage("Images/r1", "Images/r2", restartCallback);
            CCMenuItemImage item3 = new CCMenuItemImage("Images/f1", "Images/f2", nextCallback);

            CCMenu menu = new CCMenu(item1, item2, item3);
            menu.Position = new CCPoint(0, 0);
            item1.Position = new CCPoint(s.Width / 2 - 100, 30);
            item2.Position = new CCPoint(s.Width / 2, 30);
            item3.Position = new CCPoint(s.Width / 2 + 100, 30);

            AddChild(menu, 1);
        }

        public virtual string title()
        {
            return "No title";
        }

        public virtual string subtitle()
        {
            return "";
        }

        public void backCallback(object pSender)
        {
            CCScene pScene = new SchedulerTestScene();
            CCLayer pLayer = SchedulerTestScene.backSchedulerTest();

            pScene.AddChild(pLayer);
            Scene.Director.ReplaceScene(pScene);
        }

        public void nextCallback(object pSender)
        {
            CCScene pScene = new SchedulerTestScene();
            CCLayer pLayer = SchedulerTestScene.nextSchedulerTest();

            pScene.AddChild(pLayer);
            Scene.Director.ReplaceScene(pScene);
        }

        public void restartCallback(object pSender)
        {
            CCScene pScene = new SchedulerTestScene();
            CCLayer pLayer = SchedulerTestScene.restartSchedulerTest();

            pScene.AddChild(pLayer);
            Scene.Director.ReplaceScene(pScene);
        }
    }
}
