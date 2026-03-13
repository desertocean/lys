import numpy as np
import matplotlib.pyplot as plt
from matplotlib.widgets import RectangleSelector, TextBox, Button
import rasterio
from rasterio.plot import show
import tkinter as tk
from tkinter import filedialog, messagebox, Toplevel, Label, Entry, Button as TkButton
from matplotlib.patches import Rectangle
import os

# 设置中文字体
plt.rcParams['font.sans-serif'] = ['SimHei', 'Microsoft YaHei', 'WenQuanYi Zen Hei', 'Arial Unicode MS', 'DejaVu Sans']
plt.rcParams['axes.unicode_minus'] = False  # 正确显示负号


class ElevationViewer:
    def __init__(self):
        self.data = None
        self.original_data = None  # 保存原始数据用于重置
        self.transform = None
        self.crs = None
        self.filename = None
        self.rect_selector = None
        self.selection_rect = None
        self.start_point = None
        self.end_point = None
        self.aspect_ratio = 1.0  # 默认长宽比
        self.nodata_value = 200  # 默认NoData值
        self.data_array = None  # 原始数据数组（非掩码）
        self.mask_array = None  # 掩码数组

    def calculate_aspect_ratio(self):
        """计算正确的长宽比"""
        if self.transform:
            # 从仿射变换中获取像素尺寸
            pixel_width = abs(self.transform.a)  # x方向像素大小
            pixel_height = abs(self.transform.e)  # y方向像素大小

            if pixel_width > 0 and pixel_height > 0:
                self.aspect_ratio = pixel_width / pixel_height
                print(f"\n计算的长宽比: {self.aspect_ratio:.4f}")
                print(f"  像素宽度: {pixel_width:.2f} 米/像素")
                print(f"  像素高度: {pixel_height:.2f} 米/像素")
                return True
        return False

    def load_tif(self, filename):
        """加载TIF文件"""
        try:
            with rasterio.open(filename) as src:
                self.original_data = src.read(1).copy()  # 保存原始数据
                self.data_array = src.read(1).copy()  # 保存为普通数组
                self.transform = src.transform
                self.crs = src.crs
                self.filename = filename

                # 获取NoData值
                if src.nodata is not None:
                    self.nodata_value = src.nodata

                # 创建掩码数组
                self.mask_array = np.zeros_like(self.data_array, dtype=bool)
                if src.nodata is not None:
                    self.mask_array[self.data_array == src.nodata] = True

            # 创建掩码数据用于显示
            self.data = np.ma.masked_where(self.mask_array, self.data_array)

            # 计算长宽比
            self.calculate_aspect_ratio()

            print(f"\n数据信息:")
            print(f"  形状: {self.data.shape}")
            print(f"  数据类型: {self.data.dtype}")
            print(f"  最小值: {self.data.min():.2f}")
            print(f"  最大值: {self.data.max():.2f}")
            print(f"  坐标系: {self.crs}")
            print(f"  NoData值: {self.nodata_value}")
            print(f"  图像实际长宽比: {self.data.shape[1] / self.data.shape[0]:.4f}")

            return True
        except Exception as e:
            print(f"Error loading file: {e}")
            return False

    def create_selector(self):
        """创建交互式选择器"""
        # 创建图形和轴
        self.fig, self.ax = plt.subplots(figsize=(14, 10))

        # 调整图形边距，为控制面板留出空间
        plt.subplots_adjust(bottom=0.2)

        # 显示高程数据，使用正确的长宽比
        im = self.ax.imshow(self.data, cmap='terrain', aspect=self.aspect_ratio)
        plt.colorbar(im, ax=self.ax, label='高程 (米)', fraction=0.046, pad=0.04)

        # 设置标题和提示
        self.ax.set_title(
            f'高程数据 - {os.path.basename(self.filename)}\n'
            f'当前长宽比: {self.aspect_ratio:.4f} | '
            f'图像尺寸: {self.data.shape[1]}x{self.data.shape[0]} 像素\n'
            f'鼠标左键拖动选择正方形区域 | 右键拖动可平移 | 滚轮缩放',
            fontsize=12
        )

        # 创建矩形选择器
        self.rect_selector = RectangleSelector(
            self.ax,
            self.line_select_callback,
            useblit=True,
            button=[1],  # 仅使用左键
            minspanx=5,  # 最小选择宽度
            minspany=5,  # 最小选择高度
            spancoords='pixels',
            interactive=True,
            props=dict(facecolor='red', edgecolor='red', alpha=0.3, fill=True)
        )

        # 添加控制面板
        self.add_control_panel()

        # 连接键盘事件
        self.fig.canvas.mpl_connect('key_press_event', self.on_key)

        plt.show()

    def add_control_panel(self):
        """添加控制面板"""
        button_height = 0.05
        button_width = 0.08
        textbox_width = 0.1

        # 第一行按钮
        # 剪切区域按钮
        ax_cut = plt.axes([0.65, 0.08, button_width, button_height])
        self.btn_cut = Button(ax_cut, '剪切区域', color='lightblue', hovercolor='blue')
        self.btn_cut.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_cut.on_clicked(self.cut_selected_area)

        # 重置选择按钮
        ax_reset = plt.axes([0.74, 0.08, button_width, button_height])
        self.btn_reset = Button(ax_reset, '重置选择', color='lightgray', hovercolor='gray')
        self.btn_reset.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_reset.on_clicked(self.reset_selection)

        # 保存修改按钮
        ax_save = plt.axes([0.56, 0.08, button_width, button_height])
        self.btn_save = Button(ax_save, '保存修改', color='lightgreen', hovercolor='green')
        self.btn_save.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_save.on_clicked(self.save_modified_data)

        # 第二行按钮 - 编辑功能
        # 删除区域按钮
        ax_delete = plt.axes([0.65, 0.02, button_width, button_height])
        self.btn_delete = Button(ax_delete, '删除区域', color='salmon', hovercolor='red')
        self.btn_delete.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_delete.on_clicked(self.delete_selected_area)

        # 设置初始值按钮
        ax_set_value = plt.axes([0.74, 0.02, button_width, button_height])
        self.btn_set_value = Button(ax_set_value, '设置值', color='orange', hovercolor='darkorange')
        self.btn_set_value.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_set_value.on_clicked(self.set_value_dialog)

        # 恢复原始按钮
        ax_restore = plt.axes([0.56, 0.02, button_width, button_height])
        self.btn_restore = Button(ax_restore, '恢复原始', color='lightcoral', hovercolor='coral')
        self.btn_restore.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_restore.on_clicked(self.restore_original)

        # 长宽比控制区域
        # 长宽比标签
        ax_aspect_label = plt.axes([0.02, 0.05, 0.08, button_height])
        ax_aspect_label.axis('off')
        ax_aspect_label.text(0.5, 0.5, '长宽比:',
                             horizontalalignment='center',
                             verticalalignment='center',
                             fontsize=10,
                             fontproperties=plt.rcParams['font.sans-serif'][0])

        # 长宽比输入框
        ax_aspect = plt.axes([0.11, 0.05, textbox_width, button_height])
        self.aspect_textbox = TextBox(ax_aspect, '', initial=f"{self.aspect_ratio:.4f}")
        self.aspect_textbox.on_submit(self.update_aspect_ratio)

        # 应用按钮
        ax_apply = plt.axes([0.22, 0.05, 0.05, button_height])
        self.btn_apply = Button(ax_apply, '应用', color='lightyellow', hovercolor='yellow')
        self.btn_apply.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_apply.on_clicked(self.apply_aspect_ratio)

        # 自动计算按钮
        ax_auto = plt.axes([0.28, 0.05, 0.08, button_height])
        self.btn_auto = Button(ax_auto, '自动计算', color='lightcyan', hovercolor='cyan')
        self.btn_auto.label.set_fontproperties(plt.rcParams['font.sans-serif'][0])
        self.btn_auto.on_clicked(self.auto_calculate_aspect)

        # 状态显示
        self.status_ax = plt.axes([0.38, 0.05, 0.17, button_height])
        self.status_ax.axis('off')
        self.status_text = self.status_ax.text(
            0.5, 0.5, '状态: 等待选择区域...',
            horizontalalignment='center',
            verticalalignment='center',
            fontsize=9,
            fontproperties=plt.rcParams['font.sans-serif'][0],
            bbox=dict(boxstyle="round,pad=0.3", facecolor="yellow", alpha=0.2)
        )

    def update_data_display(self):
        """更新数据显示"""
        # 根据当前掩码创建新的掩码数组
        self.data = np.ma.masked_where(self.mask_array, self.data_array)

        # 更新图像
        self.ax.images[0].set_data(self.data)
        self.ax.images[0].set_clim(vmin=self.data.min(), vmax=self.data.max())
        self.fig.canvas.draw_idle()

    def set_value_dialog(self, event):
        """弹出设置值的对话框"""
        if self.start_point is None or self.end_point is None:
            messagebox.showwarning("警告", "请先选择区域！")
            self.update_status("请先选择区域")
            return

        # 创建自定义对话框
        dialog = Toplevel()
        dialog.title("设置高程值")
        dialog.geometry("300x200")
        dialog.resizable(False, False)

        # 设置对话框位置居中
        dialog.transient()
        dialog.grab_set()

        Label(dialog, text="请输入要设置的高程值:").pack(pady=10)

        value_entry = Entry(dialog, width=20)
        value_entry.pack(pady=5)
        value_entry.focus_set()

        # 显示当前区域的信息
        x1, y1 = self.start_point
        x2, y2 = self.end_point

        # 获取当前区域的非掩码值
        region_data = self.data_array[y1:y2, x1:x2]
        region_mask = self.mask_array[y1:y2, x1:x2]
        valid_data = region_data[~region_mask]

        info_text = ""
        if len(valid_data) > 0:
            info_text = f"当前区域有效值:\n  平均值: {np.mean(valid_data):.2f}\n  最小值: {np.min(valid_data):.2f}\n  最大值: {np.max(valid_data):.2f}"
        else:
            info_text = "当前区域全为NoData"

        Label(dialog, text=info_text, justify=tk.LEFT).pack(pady=10)

        # NoData选项
        use_nodata_var = tk.BooleanVar()
        tk.Checkbutton(dialog, text="设置为NoData", variable=use_nodata_var).pack(pady=5)

        def on_ok():
            if use_nodata_var.get():
                dialog.destroy()
                self.delete_selected_area(None)
            else:
                try:
                    value = float(value_entry.get())
                    dialog.destroy()
                    self.set_selected_value(value)
                except ValueError:
                    messagebox.showerror("错误", "请输入有效的数字！")

        def on_cancel():
            dialog.destroy()

        # 按钮框架
        button_frame = tk.Frame(dialog)
        button_frame.pack(pady=10)

        TkButton(button_frame, text="确定", command=on_ok, width=8).pack(side=tk.LEFT, padx=5)
        TkButton(button_frame, text="取消", command=on_cancel, width=8).pack(side=tk.LEFT, padx=5)

        # 等待对话框关闭
        dialog.wait_window()

    def set_selected_value(self, value):
        """将选中区域设置为指定值"""
        if self.start_point is None or self.end_point is None:
            return

        x1, y1 = self.start_point
        x2, y2 = self.end_point

        # 确保坐标在有效范围内
        y1 = max(0, min(y1, self.data_array.shape[0]))
        y2 = max(0, min(y2, self.data_array.shape[0]))
        x1 = max(0, min(x1, self.data_array.shape[1]))
        x2 = max(0, min(x2, self.data_array.shape[1]))

        # 设置新值到数据数组
        self.data_array[y1:y2, x1:x2] = value
        # 取消这些位置的掩码
        self.mask_array[y1:y2, x1:x2] = False

        # 更新显示
        self.update_data_display()
        self.update_status(f"已将区域设置为: {value:.2f}")
        print(f"已将选择区域设置为: {value:.2f}")

    def delete_selected_area(self, event):
        """删除选中区域（设置为NoData）"""
        if self.start_point is None or self.end_point is None:
            messagebox.showwarning("警告", "请先选择区域！")
            self.update_status("请先选择区域")
            return

        # 确认对话框
        result = messagebox.askyesno("确认", f"确定要删除选中的区域吗？\n(将设置为NoData值: {self.nodata_value})")
        if not result:
            return

        x1, y1 = self.start_point
        x2, y2 = self.end_point

        # 确保坐标在有效范围内
        y1 = max(0, min(y1, self.data_array.shape[0]))
        y2 = max(0, min(y2, self.data_array.shape[0]))
        x1 = max(0, min(x1, self.data_array.shape[1]))
        x2 = max(0, min(x2, self.data_array.shape[1]))

        # 设置为NoData值
        self.data_array[y1:y2, x1:x2] = self.nodata_value
        # 设置掩码
        self.mask_array[y1:y2, x1:x2] = True

        # 更新显示
        self.update_data_display()
        self.update_status(f"已删除选中区域 (设置为NoData)")
        print(f"已删除选择区域 (设置为NoData: {self.nodata_value})")

    def restore_original(self, event):
        """恢复原始数据"""
        result = messagebox.askyesno("确认", "确定要恢复原始数据吗？\n所有修改将会丢失。")
        if not result:
            return

        # 恢复原始数据
        with rasterio.open(self.filename) as src:
            self.data_array = src.read(1).copy()

            # 重新创建掩码
            self.mask_array = np.zeros_like(self.data_array, dtype=bool)
            if src.nodata is not None:
                self.mask_array[self.data_array == src.nodata] = True

        # 清除选择
        self.reset_selection(event)

        # 更新显示
        self.update_data_display()
        self.update_status("已恢复原始数据")
        print("已恢复原始数据")

    def save_modified_data(self, event):
        """保存修改后的完整数据"""
        # 选择保存位置
        root = tk.Tk()
        root.withdraw()

        # 生成默认文件名
        default_name = os.path.splitext(os.path.basename(self.filename))[0] + "_modified.tif"

        save_path = filedialog.asksaveasfilename(
            title="保存修改后的数据",
            initialfile=default_name,
            defaultextension=".tif",
            filetypes=[("GeoTIFF文件", "*.tif"), ("TIFF文件", "*.tiff"), ("所有文件", "*.*")]
        )
        root.destroy()

        if save_path:
            try:
                # 准备数据
                if np.ma.is_masked(self.data):
                    data_to_write = self.data.filled(self.nodata_value)
                else:
                    data_to_write = self.data_array

                # 保存为GeoTIFF
                with rasterio.open(
                        save_path,
                        'w',
                        driver='GTiff',
                        height=self.data_array.shape[0],
                        width=self.data_array.shape[1],
                        count=1,
                        dtype=data_to_write.dtype,
                        crs=self.crs,
                        transform=self.transform,
                        nodata=self.nodata_value
                ) as dst:
                    dst.write(data_to_write, 1)

                messagebox.showinfo("成功", f"文件已保存到:\n{save_path}")
                self.update_status(f"已保存到: {os.path.basename(save_path)}")
                print(f"文件已保存: {save_path}")

            except Exception as e:
                messagebox.showerror("错误", f"保存文件失败:\n{str(e)}")
                print(f"保存失败: {e}")

    def cut_selected_area(self, event):
        """剪切选中的正方形区域（仅用于查看）"""
        if self.start_point is None or self.end_point is None:
            messagebox.showwarning("警告", "请先选择区域！")
            self.update_status("请先选择区域")
            return

        # 提取选中的矩形区域
        x1, y1 = self.start_point
        x2, y2 = self.end_point

        # 确保坐标在有效范围内
        y1 = max(0, min(y1, self.data.shape[0]))
        y2 = max(0, min(y2, self.data.shape[0]))
        x1 = max(0, min(x1, self.data.shape[1]))
        x2 = max(0, min(x2, self.data.shape[1]))

        # 剪切数据（使用掩码后的数据）
        self.cut_data = self.data[y1:y2, x1:x2].copy()

        # 显示剪切区域信息
        print(f"\n剪切完成:")
        print(f"  剪切区域形状: {self.cut_data.shape}")
        if not np.ma.is_masked(self.cut_data) or not self.cut_data.mask.all():
            valid_data = self.cut_data.compressed() if np.ma.is_masked(self.cut_data) else self.cut_data
            if len(valid_data) > 0:
                print(f"  最小值: {valid_data.min():.2f}")
                print(f"  最大值: {valid_data.max():.2f}")
                print(f"  平均值: {valid_data.mean():.2f}")
            else:
                print("  全为NoData区域")

        # 显示剪切的区域
        self.show_cut_area()
        self.update_status(f"已剪切 {self.cut_data.shape[1]}x{self.cut_data.shape[0]} 区域")

    def show_cut_area(self):
        """显示剪切的区域"""
        fig_cut, (ax1, ax2) = plt.subplots(1, 2, figsize=(16, 6))

        # 设置中文字体
        for ax in [ax1, ax2]:
            for label in ax.get_xticklabels() + ax.get_yticklabels():
                label.set_fontproperties(plt.rcParams['font.sans-serif'][0])

        # 原始数据上标记选择区域（使用相同的长宽比）
        im1 = ax1.imshow(self.data, cmap='terrain', aspect=self.aspect_ratio)
        ax1.add_patch(Rectangle(
            self.start_point,
            self.end_point[0] - self.start_point[0],
            self.end_point[1] - self.start_point[1],
            fill=False, edgecolor='red', linewidth=2
        ))
        ax1.set_title('原始数据 (红色框为选择区域)', fontproperties=plt.rcParams['font.sans-serif'][0])
        plt.colorbar(im1, ax=ax1, label='高程 (米)', fraction=0.046, pad=0.04)

        # 剪切的区域（保持相同的长宽比）
        im2 = ax2.imshow(self.cut_data, cmap='terrain', aspect=self.aspect_ratio)
        ax2.set_title(f'剪切区域\n{self.cut_data.shape[1]}x{self.cut_data.shape[0]} 像素',
                      fontproperties=plt.rcParams['font.sans-serif'][0])
        plt.colorbar(im2, ax=ax2, label='高程 (米)', fraction=0.046, pad=0.04)

        # 显示统计信息
        if not np.ma.is_masked(self.cut_data) or not self.cut_data.mask.all():
            valid_data = self.cut_data.compressed() if np.ma.is_masked(self.cut_data) else self.cut_data
            if len(valid_data) > 0:
                stats_text = f'最小值: {valid_data.min():.2f} m\n最大值: {valid_data.max():.2f} m\n平均值: {valid_data.mean():.2f} m'
            else:
                stats_text = '全为NoData区域'
        else:
            stats_text = '全为NoData区域'

        if self.transform:
            real_width = (self.end_point[0] - self.start_point[0]) * abs(self.transform.a)
            real_height = (self.end_point[1] - self.start_point[1]) * abs(self.transform.e)
            stats_text += f'\n实际尺寸: {real_width:.1f} x {real_height:.1f} 米'

        ax2.text(0.02, 0.98, stats_text, transform=ax2.transAxes,
                 fontsize=10, verticalalignment='top',
                 fontproperties=plt.rcParams['font.sans-serif'][0],
                 bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.5))

        plt.tight_layout()
        plt.show()

    def update_aspect_ratio(self, text):
        """从输入框更新长宽比"""
        try:
            new_aspect = float(text)
            if new_aspect > 0:
                self.aspect_ratio = new_aspect
                self.apply_aspect_ratio()
                return True
            else:
                print("错误: 请输入正数！")
                self.aspect_textbox.set_val(f"{self.aspect_ratio:.4f}")
                return False
        except ValueError:
            print("错误: 请输入有效的数字！")
            self.aspect_textbox.set_val(f"{self.aspect_ratio:.4f}")
            return False

    def apply_aspect_ratio(self, event=None):
        """应用新的长宽比"""
        # 更新图像显示
        self.ax.images[0].set_extent([
            0, self.data.shape[1],
            self.data.shape[0], 0
        ])
        self.ax.set_aspect(self.aspect_ratio)

        # 更新标题
        self.ax.set_title(
            f'高程数据 - {os.path.basename(self.filename)}\n'
            f'当前长宽比: {self.aspect_ratio:.4f} | '
            f'图像尺寸: {self.data.shape[1]}x{self.data.shape[0]} 像素\n'
            f'鼠标左键拖动选择正方形区域 | 右键拖动可平移 | 滚轮缩放',
            fontsize=12
        )

        self.fig.canvas.draw_idle()
        self.update_status(f"长宽比已设置为: {self.aspect_ratio:.4f}")

    def auto_calculate_aspect(self, event=None):
        """自动计算长宽比"""
        if self.calculate_aspect_ratio():
            self.aspect_textbox.set_val(f"{self.aspect_ratio:.4f}")
            self.apply_aspect_ratio()
            self.update_status(f"自动计算长宽比: {self.aspect_ratio:.4f}")

    def line_select_callback(self, eclick, erelease):
        """矩形选择回调函数"""
        x1, y1 = int(eclick.xdata), int(eclick.ydata)
        x2, y2 = int(erelease.xdata), int(erelease.ydata)

        # 确保坐标在图像范围内
        x1 = max(0, min(x1, self.data.shape[1]))
        x2 = max(0, min(x2, self.data.shape[1]))
        y1 = max(0, min(y1, self.data.shape[0]))
        y2 = max(0, min(y2, self.data.shape[0]))

        # 保存选择区域（确保x1<x2, y1<y2）
        self.start_point = (min(x1, x2), min(y1, y2))
        self.end_point = (max(x1, x2), max(y1, y2))

        # 清除之前的矩形
        if self.selection_rect:
            self.selection_rect.remove()

        # 绘制新的选择矩形
        width = self.end_point[0] - self.start_point[0]
        height = self.end_point[1] - self.start_point[1]
        self.selection_rect = Rectangle(
            self.start_point, width, height,
            fill=False, edgecolor='red', linewidth=2, linestyle='--'
        )
        self.ax.add_patch(self.selection_rect)

        # 显示选区信息
        print(f"\n选择区域:")
        print(f"  左上角: ({self.start_point[0]}, {self.start_point[1]})")
        print(f"  右下角: ({self.end_point[0]}, {self.end_point[1]})")
        print(f"  宽度: {width} 像素")
        print(f"  高度: {height} 像素")

        # 计算实际地理距离
        if self.transform:
            real_width = width * abs(self.transform.a)
            real_height = height * abs(self.transform.e)
            print(f"  实际宽度: {real_width:.2f} 米")
            print(f"  实际高度: {real_height:.2f} 米")

        self.fig.canvas.draw_idle()
        self.update_status(f"已选择 {width}x{height} 像素区域")

    def update_status(self, message):
        """更新状态显示"""
        self.status_text.set_text(f'状态: {message}')
        self.fig.canvas.draw_idle()

    def reset_selection(self, event):
        """重置选择"""
        if self.selection_rect:
            self.selection_rect.remove()
            self.selection_rect = None
        self.start_point = None
        self.end_point = None
        if self.rect_selector:
            self.rect_selector.set_active(True)
        self.fig.canvas.draw_idle()
        self.update_status("已重置选择")
        print("选择已重置")

    def on_key(self, event):
        """键盘事件处理"""
        if event.key == 'escape':
            self.reset_selection(event)
        elif event.key == 'c' or event.key == 'C':
            self.cut_selected_area(event)
        elif event.key == 's' or event.key == 'S':
            self.save_modified_data(event)
        elif event.key == 'a' or event.key == 'A':
            self.auto_calculate_aspect()
        elif event.key == 'd' or event.key == 'D':
            self.delete_selected_area(event)
        elif event.key == 'v' or event.key == 'V':
            self.set_value_dialog(event)
        elif event.key == 'r' or event.key == 'R':
            self.restore_original(event)


def main():
    """主函数"""
    print("=" * 50)
    print("高程数据编辑工具")
    print("=" * 50)

    # 创建tkinter根窗口用于文件对话框
    root = tk.Tk()
    root.withdraw()

    # 选择TIF文件
    file_path = filedialog.askopenfilename(
        title="选择高程TIF文件",
        filetypes=[
            ("GeoTIFF文件", "*.tif *.tiff"),
            ("所有文件", "*.*")
        ]
    )

    if not file_path:
        print("未选择文件，程序退出。")
        return

    print(f"选择的文件: {file_path}")

    # 创建查看器实例
    viewer = ElevationViewer()

    # 加载TIF文件
    if viewer.load_tif(file_path):
        print("\n文件加载成功！")
        print("使用说明:")
        print("  - 鼠标左键拖动: 选择正方形区域")
        print("  - 鼠标右键拖动: 平移视图")
        print("  - 鼠标滚轮: 缩放视图")
        print("  - ESC键: 重置选择")
        print("  - C键: 查看剪切区域")
        print("  - S键: 保存修改")
        print("  - D键: 删除选中区域")
        print("  - V键: 设置自定义值")
        print("  - R键: 恢复原始数据")
        print("  - A键: 自动计算长宽比")
        print("=" * 50)

        # 创建选择器
        viewer.create_selector()
    else:
        print("文件加载失败。")


if __name__ == "__main__":
    main()