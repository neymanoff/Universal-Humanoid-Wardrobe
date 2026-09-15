# Blender 4.x Character Preparation Guide for Universal Humanoid Wardrobe

This guide provides step-by-step instructions for 3D artists and game developers to prepare Humanoid character models in Blender for use with the **Universal Humanoid Wardrobe** package.

It covers both anti-clipping workflows supported by the system:
1. **Approach A: Modular Sub-Meshes** (Splitting geometry into toggleable body regions).
2. **Approach B: Shrink Shape Keys / BlendShapes** (Adding inward morphs for seamless, monolithic bodies).

---

## 1. Prerequisites & Rig Hygiene

Before preparing clothing or body masks, ensure your base character model satisfies standard Unity Humanoid requirements:

1. **Scale & Transforms**:
   * Select your character mesh and armature in `Object Mode`.
   * Press `Ctrl + A` $\rightarrow$ **Apply All Transforms** (Location, Rotation, Scale must be `1.0, 1.0, 1.0` with `0, 0, 0` rotation).
2. **Rest Pose**:
   * Align your character in a canonical **T-Pose** or **A-Pose** facing the negative Y axis (standard Blender front view `Numpad 1`).
3. **Vertex Group Normalization**:
   * In `Weight Paint` mode, open `Weights` $\rightarrow$ **Normalize All** (Lock Active: unchecked). Ensure no vertex has a total bone weight sum exceeding `1.0`.

---

## 2. Approach A: Modular Sub-Meshes Workflow

This approach is the industry standard for RPGs with varied armor coverage (e.g. *Dark Souls*, *Monster Hunter*). The base body is divided into discrete pieces, allowing `WardrobeManager` to disable occluded regions entirely with **zero performance overhead**.

### Step 1: Define Your Cut Lines (Edge Loops)
1. In `Edit Mode` (`Tab`), inspect the edge loops corresponding to our `BodyPartMask` boundaries:
   * **UpperTorso**: From clavicles/neck down to the bottom of the ribcage.
   * **LowerTorso**: From bottom of the ribcage down to the pelvic crease (iliac crest).
   * **UpperArms**: From shoulder joint down to mid-bicep (above elbow).
   * **LowerArms**: From elbow down to the wrist seam.
   * **Hands**: Wrist seam to fingertips.
   * **UpperLegs**: Pelvic crease down to above knee.
   * **LowerLegs**: Knee down to ankle crease.
   * **Feet**: Ankle crease to toes.

> [!TIP]
> Place cut lines where clothing borders naturally sit (wrists, ankles, waistline, collarbone) to hide any possible boundary seam.

### Step 2: Separate Meshes While Preserving Weights
1. In `Edit Mode`, select the faces of the region you wish to separate (e.g. the chest and back for `UpperTorso`).
2. Press `P` $\rightarrow$ **Selection**.
3. Repeat for each anatomical region.
4. Rename the resulting objects cleanly in the Outliner:
   * `Body_Head`
   * `Body_UpperTorso`
   * `Body_LowerTorso`
   * `Body_UpperArms`
   * `Body_LowerArms`
   * `Body_Hands`
   * `Body_UpperLegs`
   * `Body_LowerLegs`
   * `Body_Feet`

> [!NOTE]
> Separating by selection in Blender **preserves all existing vertex groups, weight painting, and Armature modifiers**. You do not need to re-rig the character.

### Step 3: Prevent Seam Artifacts (Normal Transfer)
When vertices along a cut boundary have independent split normals, a visible dark seam can appear when the character is naked. To ensure seamless lighting:
1. Select one of the separated body pieces (e.g. `Body_LowerTorso`).
2. Add a **Data Transfer** modifier.
3. Set **Source** to the adjacent piece (or an untouched duplicate of the original unseparated body).
4. Check **Face Corner Data** $\rightarrow$ enable **Custom Normals**.
5. Click **Generate Data Layers** and apply the modifier before export.

---

## 3. Approach B: Shrink Shape Keys (BlendShapes) Workflow

Use this approach if your character has tattoos, realistic bare skin, or if you prefer a single continuous, uncut mesh. `WardrobeManager` automatically dials these morphs to 100% when armor is equipped to tuck the skin inside the armor geometry.

### Step 1: Add the Basis Shape Key
1. Select your character mesh in `Object Mode`.
2. Go to the **Object Data Properties** tab (green triangle icon in the properties panel).
3. Expand the **Shape Keys** section.
4. Click the **`+`** button once to create the **`Basis`** key (this represents the default un-deformed body).

### Step 2: Create Shrink Shape Keys
1. Click the **`+`** button again to create a new key.
2. Double-click the key name and rename it following our standard naming conventions:
   * `Shrink_Torso` (deflates chest and abdomen)
   * `Shrink_Chest` (deflates breasts/pectoral area only)
   * `Shrink_Belly` (deflates stomach/waist only)
   * `Shrink_Arms` (thins upper and lower arms)
   * `Shrink_Legs` (thins thighs and calves)
3. Set the **Value** slider of the newly created key to `1.0` while sculpting/editing so you can observe the effect in real-time.

### Step 3: Deform the Vertices Inward
Choose one of two methods:

#### Method 1: Edit Mode Shrink/Fatten (Precision)
1. Switch to `Edit Mode` (`Tab`).
2. Select the vertices in the target region (e.g. chest/stomach).
3. Press `Alt + S` (**Shrink/Fatten**) and move your mouse inward slightly to deflate the geometry by 2–4 cm.
4. Use `O` (Proportional Editing) with a `Smooth` falloff to ensure a gradual transition toward the boundaries.

#### Method 2: Sculpt Mode Deflate Brush (Organic Control)
1. Switch to `Sculpt Mode`.
2. Select the **Deflate / Inflate** brush.
3. Hold `Ctrl` while brushing over the mesh to push vertices inward smoothly.

### Step 4: Verify the Slider
1. Switch back to `Object Mode`.
2. Drag the Value slider between `0.0` (normal body) and `1.0` (deflated body).
3. Verify that the geometry shrinks smoothly inward without inverting faces or creating self-intersections.

---

## 4. Exporting from Blender to Unity (FBX Settings)

1. Select your Armature and all Body sub-meshes.
2. Go to **File $\rightarrow$ Export $\rightarrow$ FBX (.fbx)**.
3. Configure the following export options:
   * **Include**:
     * Check **Limit to: Selected Objects**.
     * Object Types: Select **Armature** and **Mesh**.
   * **Transform**:
     * Apply Scalings: **FBX All**.
     * Forward: **-Z Forward**.
     * Up: **Y Up**.
     * Check **Apply Unit**.
   * **Geometry**:
     * Smoothing: **Face** or **Normals Only**.
     * Check **Apply Modifiers** (Ensure this does not destroy shape keys; for Shape Keys export, leave unapplied or check Apply Modifiers while preserving shape keys).
   * **Armature**:
     * Primary Bone Axis: **Y Axis**.
     * Secondary Bone Axis: **X Axis**.
     * **Uncheck** *Add Leaf Bones* (Crucial: prevents Blender from adding extra dummy bones at bone tips).
   * **Bake Animation**:
     * Uncheck if exporting a base character model without animations.

---

## 5. Setting up in Unity 6 (`WardrobeManager`)

After importing the FBX into Unity:

### For Modular Characters (Approach A):
1. Select the character GameObject in your scene.
2. In the `WardrobeManager` inspector:
   * Under **Body Masking & Anti-Clipping**, add entries to **Modular Body Parts**:
     * `Part`: Select anatomical zone (e.g. `UpperTorso`).
     * `Renderer`: Drag the corresponding child `SkinnedMeshRenderer` (e.g. `Body_UpperTorso`).
3. Now, whenever an equipped `WardrobeItemSO` has `UpperTorso` flagged in `hiddenBodyParts`, that renderer is automatically disabled!

### For Monolithic Characters with Shape Keys (Approach B):
1. In the `WardrobeManager` inspector:
   * Under **Morph Targets**, add your character's `SkinnedMeshRenderer`.
2. In your `WardrobeItemSO` assets:
   * In **Shrink Blend Shapes**, enter the exact names created in Blender (e.g. `Shrink_Torso`, `Shrink_Arms`).
3. Whenever the item is equipped, `WardrobeManager` automatically sets those blendshapes to `100%` weight, resetting them to `0%` upon unequip!
