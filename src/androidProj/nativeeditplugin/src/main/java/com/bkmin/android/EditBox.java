package com.bkmin.android;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import android.app.Activity;
import android.content.Context;
import android.graphics.Color;
import android.graphics.Rect;
import android.graphics.Typeface;

import android.text.Editable;
import android.text.InputType;
import android.text.TextWatcher;
import android.util.Log;
import android.util.SparseArray;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.View;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;
import android.widget.RelativeLayout;
import android.widget.TextView;

import com.redmadrobot.inputmask.MaskedTextChangedListener;
import com.redmadrobot.inputmask.helper.AffinityCalculationStrategy;

import java.util.ArrayList;
import java.util.List;

public class EditBox {

    // Simplest way to notify the EditBox about the application lifecycle.
    class EditTextLifeCycle extends EditText
    {
        EditBox observerBox;
        public EditTextLifeCycle(Context context, EditBox box)
        {
            super(context);
            this.observerBox = box;
        }

        @Override
        public void onWindowFocusChanged(boolean hasWindowFocus)
        {
            super.onWindowFocusChanged(hasWindowFocus);
            if (!hasWindowFocus)
                observerBox.notifyFocusChanged(hasWindowFocus);
        }
    }

    private EditTextLifeCycle edit;
    private final RelativeLayout layout;
    private int tag;
    private int characterLimit;

    private static SparseArray<EditBox> mapEditBox = null;
    private static final String MSG_CREATE = "CreateEdit";
    private static final String MSG_REMOVE = "RemoveEdit";
    private static final String MSG_SET_TEXT = "SetText";
    private static final String MSG_SET_RECT = "SetRect";
    private static final String MSG_SET_TEXTSIZE = "SetTextSize";
    private static final String MSG_SET_FOCUS = "SetFocus";
    private static final String MSG_SET_VISIBLE = "SetVisible";
    private static final String MSG_TEXT_CHANGE = "TextChange";
    private static final String MSG_TEXT_BEGIN_EDIT = "TextBeginEdit";
    private static final String MSG_TEXT_END_EDIT = "TextEndEdit";
    private static final String MSG_ANDROID_KEY_DOWN = "AndroidKeyDown";
    private static final String MSG_RETURN_PRESSED = "ReturnPressed";

    private static final String APPLY_MASK_KEY = "applyMask";
    private static final String AFFINE_MASKS_KEY = "affineMasks";

    public static void processRecvJsonMsg(int nSenderId, final String strJson)
    {
        if (mapEditBox == null) mapEditBox = new SparseArray<>();

        try
        {
            JSONObject jsonMsg = new JSONObject(strJson);
            String msg = jsonMsg.getString("msg");

            if (msg.equals(MSG_CREATE))
            {
                EditBox nb = new EditBox(NativeEditPlugin.mainLayout);
                nb.Create(nSenderId, jsonMsg);
                mapEditBox.append(nSenderId, nb);
            }
            else
            {
                EditBox eb =  mapEditBox.get(nSenderId);
                if (eb != null) {
                    eb.processJsonMsg(jsonMsg);
                }
                else
                {
                    Log.e(NativeEditPlugin.LOG_TAG, "EditBox not found, id : " + nSenderId);
                }
            }


        } catch (JSONException e)
        {
        }
    }

    private EditBox(RelativeLayout mainLayout)
    {
        layout = mainLayout;
        edit = null;
    }

    private void showKeyboard(boolean isShow)
    {
        InputMethodManager imm = (InputMethodManager) NativeEditPlugin.unityActivity.getSystemService(Activity.INPUT_METHOD_SERVICE);

        View rootView = NativeEditPlugin.unityActivity.getWindow().getDecorView();

        if (isShow)
        {
            imm.showSoftInput(edit, InputMethodManager.SHOW_FORCED);
        }
        else
        {
            rootView.clearFocus();
            imm.hideSoftInputFromWindow(edit.getWindowToken(), 0);
        }
    }

    private void notifyFocusChanged(boolean hasWindowFocus)
    {
        if(!hasWindowFocus)
            showKeyboard(false);
    }

    private void processJsonMsg(JSONObject jsonMsg)
    {
        try
        {
            String msg = jsonMsg.getString("msg");

            switch (msg) {
                case MSG_REMOVE:
                    this.Remove();
                    break;
                case MSG_SET_TEXT:
                    String text = jsonMsg.getString("text");
                    this.SetText(text);
                    break;
                case MSG_SET_RECT:
                    this.SetRect(jsonMsg);
                    break;
                case MSG_SET_TEXTSIZE:
                    this.SetTextSize(jsonMsg);
                    break;
                case MSG_SET_FOCUS:
                    boolean isFocus = jsonMsg.getBoolean("isFocus");
                    this.SetFocus(isFocus);
                    break;
                case MSG_SET_VISIBLE:
                    boolean isVisible = jsonMsg.getBoolean("isVisible");
                    this.SetVisible(isVisible);
                    break;
                case MSG_ANDROID_KEY_DOWN:
                    String strKey = jsonMsg.getString("key");
                    this.OnForceAndroidKeyDown(strKey);
                    break;
            }

        } catch (Exception e)
        {
            Log.e(NativeEditPlugin.LOG_TAG, "Exception in native EditBox.processJsonMsg(). " + e.getMessage());
        }
    }

    private void SendJsonToUnity(JSONObject jsonToUnity)
    {
        try
        {
            jsonToUnity.put("senderId", this.tag);
        }
        catch(JSONException e) {}
        NativeEditPlugin.SendUnityMessage(jsonToUnity);
    }

    private void Create(int _tag, JSONObject jsonObj)
    {
        this.tag = _tag;

        try {
            String placeHolder = jsonObj.getString("placeHolder");

            String font = jsonObj.getString("font");
            double fontSize = jsonObj.getDouble("fontSize");

            double x = jsonObj.getDouble("x") * (double) layout.getWidth();
            double y = jsonObj.getDouble("y") * (double) layout.getHeight();
            double width = jsonObj.getDouble("width") * (double) layout.getWidth();
            double height = jsonObj.getDouble("height") * (double) layout.getHeight();
            characterLimit = jsonObj.getInt("characterLimit");

            int textColor_r = (int) (255.0f * jsonObj.getDouble("textColor_r"));
            int textColor_g = (int) (255.0f * jsonObj.getDouble("textColor_g"));
            int textColor_b = (int) (255.0f * jsonObj.getDouble("textColor_b"));
            int textColor_a = (int) (255.0f * jsonObj.getDouble("textColor_a"));
            int backColor_r = (int) (255.0f * jsonObj.getDouble("backColor_r"));
            int backColor_g = (int) (255.0f * jsonObj.getDouble("backColor_g"));
            int backColor_b = (int) (255.0f * jsonObj.getDouble("backColor_b"));
            int backColor_a = (int) (255.0f * jsonObj.getDouble("backColor_a"));
            int placeHolderColor_r = (int) (255.0f * jsonObj.getDouble("placeHolderColor_r"));
            int placeHolderColor_g = (int) (255.0f * jsonObj.getDouble("placeHolderColor_g"));
            int placeHolderColor_b = (int) (255.0f * jsonObj.getDouble("placeHolderColor_b"));
            int placeHolderColor_a = (int) (255.0f * jsonObj.getDouble("placeHolderColor_a"));

            String contentType = jsonObj.getString("contentType");
            String inputType = jsonObj.optString("inputType");
            String keyboardType = jsonObj.optString("keyboardType");
            String returnKeyType = jsonObj.getString("return_key_type");

            String alignment = jsonObj.getString("align");
            boolean multiline = jsonObj.getBoolean("multiline");

            edit = new EditTextLifeCycle(NativeEditPlugin.unityActivity.getApplicationContext(), this);

            // It's important to set this first as it resets some things, for example character hiding if content type is password.
            edit.setSingleLine(!multiline);

            edit.setId(0);
            edit.setText("");
            edit.setHint(placeHolder);

            Rect rect = new Rect((int) x, (int) y, (int) (x + width), (int) (y + height));
            RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams(rect.width(), rect.height());
            lp.setMargins(rect.left, rect.top, 0, 0);
            edit.setLayoutParams(lp);
            edit.setPadding(0, 0, 0, 0);

            int editInputType = 0;
            switch (contentType) {
                case "Standard" : editInputType |= InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES; break; // This is default behaviour
                case "Autocorrected" : editInputType |= InputType.TYPE_CLASS_TEXT  | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES | InputType.TYPE_TEXT_FLAG_AUTO_CORRECT; break;
                case "IntegerNumber" : editInputType |= InputType.TYPE_CLASS_NUMBER; break;
                case "DecimalNumber" : editInputType |= InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_DECIMAL; break;
                case "Alphanumeric" : editInputType |= InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES; break; // This is default behaviour
                case "Name" : editInputType |= InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PERSON_NAME; break;
                case "EmailAddress" : editInputType |= InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS; break;
                case "Password" : editInputType |= InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD; break;
                case "Pin" : editInputType |= InputType.TYPE_CLASS_PHONE; break;

                case "Custom" : // We need more details
                    switch (keyboardType) {
                        case "ASCIICapable" : editInputType = InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS; break;
                        case "NumbersAndPunctuation" : editInputType = InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_DECIMAL |InputType.TYPE_NUMBER_FLAG_SIGNED; break;
                        case "URL" : editInputType = InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS | InputType.TYPE_TEXT_VARIATION_URI; break;
                        case "NumberPad" : editInputType = InputType.TYPE_CLASS_NUMBER;  break;
                        case "PhonePad" : editInputType = InputType.TYPE_CLASS_PHONE;  break;
                        case "NamePhonePad" : editInputType = InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PERSON_NAME; break;
                        case "EmailAddress" : editInputType = InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS; break;
                        default :  editInputType = InputType.TYPE_CLASS_TEXT;
                    }

                    if (multiline) editInputType  |=  InputType.TYPE_TEXT_FLAG_MULTI_LINE | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES;

                    switch (inputType) {
                        case "AutoCorrect" : editInputType |=  InputType.TYPE_TEXT_FLAG_AUTO_CORRECT; break;
                        case "Password" : editInputType |=  InputType.TYPE_NUMBER_VARIATION_PASSWORD | InputType.TYPE_TEXT_VARIATION_PASSWORD ; break;
                    }
                    break;

                default : editInputType |= InputType.TYPE_CLASS_TEXT; break; // No action

            }

            edit.setInputType(editInputType);

            int gravity = 0;
            switch (alignment) {
                case "UpperLeft":
                    gravity = Gravity.TOP | Gravity.LEFT;
                    break;
                case "UpperCenter":
                    gravity = Gravity.TOP | Gravity.CENTER_HORIZONTAL;
                    break;
                case "UpperRight":
                    gravity = Gravity.TOP | Gravity.RIGHT;
                    break;
                case "MiddleLeft":
                    gravity = Gravity.CENTER_VERTICAL | Gravity.LEFT;
                    break;
                case "MiddleCenter":
                    gravity = Gravity.CENTER_VERTICAL | Gravity.CENTER_HORIZONTAL;
                    break;
                case "MiddleRight":
                    gravity = Gravity.CENTER_VERTICAL | Gravity.RIGHT;
                    break;
                case "LowerLeft":
                    gravity = Gravity.BOTTOM | Gravity.LEFT;
                    break;
                case "LowerCenter":
                    gravity = Gravity.BOTTOM | Gravity.CENTER_HORIZONTAL;
                    break;
                case "LowerRight":
                    gravity = Gravity.BOTTOM | Gravity.RIGHT;
                    break;
            }

            int imeOptions = EditorInfo.IME_FLAG_NO_EXTRACT_UI;
            if (returnKeyType.equals("Next")) {
                imeOptions |= EditorInfo.IME_ACTION_NEXT;
            }
            else if (returnKeyType.equals("Done")) {
                imeOptions |= EditorInfo.IME_ACTION_DONE;
            }
            edit.setImeOptions(imeOptions);

            edit.setGravity(gravity);

            edit.setTextSize(TypedValue.COMPLEX_UNIT_PX, (float) fontSize);
            edit.setTextColor(Color.argb(textColor_a, textColor_r, textColor_g, textColor_b));
            edit.setBackgroundColor(Color.argb(backColor_a, backColor_r, backColor_g, backColor_b));
            edit.setHintTextColor(Color.argb(placeHolderColor_a, placeHolderColor_r, placeHolderColor_g, placeHolderColor_b));

            if (font != null && !font.isEmpty()) {
                Typeface tf = Typeface.create(font, Typeface.NORMAL);
                edit.setTypeface(tf);
            }

            final EditBox eb = this;
            edit.setOnFocusChangeListener(new View.OnFocusChangeListener() {
                @Override
                public void onFocusChange(View v, boolean hasFocus) {

                    // your action here
                    JSONObject msgTextEndJSON = new JSONObject();
                    try
                    {
                        msgTextEndJSON.put("msg", hasFocus ? MSG_TEXT_BEGIN_EDIT : MSG_TEXT_END_EDIT);
                        msgTextEndJSON.put("text", eb.GetText());
                    }
                    catch(JSONException e) {}
                    eb.SendJsonToUnity(msgTextEndJSON);

                    SetFocus(hasFocus);
                }
            });

            TextWatcher editTextWatcher = getEditTextWatcher(jsonObj, eb);
            edit.addTextChangedListener(editTextWatcher);

            edit.setOnEditorActionListener(new TextView.OnEditorActionListener() {
                @Override
                public boolean onEditorAction(TextView v, int actionId, KeyEvent event) {
                    if ((actionId == EditorInfo.IME_ACTION_DONE) || (actionId == EditorInfo.IME_ACTION_NEXT)) {
                        JSONObject jsonToUnity = new JSONObject();
                        try
                        {
                            jsonToUnity.put("msg", MSG_RETURN_PRESSED);
                        }
                        catch(JSONException e) {}

                        eb.SendJsonToUnity(jsonToUnity);
                        return true;
                    }
                    return false;
                }
            });

            layout.addView(edit);

        } catch (JSONException e)
        {
            Log.i(NativeEditPlugin.LOG_TAG, String.format("Create editbox error %s", e.getMessage()));
        }
    }

    private TextWatcher getEditTextWatcher(JSONObject jsonObj, final EditBox editBox) throws JSONException {

        boolean applyMask = jsonObj.has(APPLY_MASK_KEY) && jsonObj.getBoolean(APPLY_MASK_KEY);

        if (applyMask) {
            String primaryMask = jsonObj.getString("primaryMask");
            JSONArray affineMasks = jsonObj.isNull(AFFINE_MASKS_KEY) ? new JSONArray() : jsonObj.getJSONArray(AFFINE_MASKS_KEY);
            int affinityStrategy = jsonObj.getInt("affinityStrategy");
            boolean useCustomPlaceholder = jsonObj.getBoolean("useCustomPlaceholder");
            String customPlaceholder = jsonObj.getString("customPlaceholder");

            // Convert JSONArray to List
            List<String> affineMasksList = new ArrayList<>();
            for (int i = 0; i < affineMasks.length(); i++) {
                try {
                    affineMasksList.add(affineMasks.getString(i));
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }

            final MaskedTextChangedListener maskedTextChangedListener = new MaskedTextChangedListener(
                    primaryMask,
                    affineMasksList,
                    GetAffinityCalculationStrategy(affinityStrategy),
                    true,
                    edit,
                    null,
                    new MaskedTextChangedListener.ValueListener() {
                        @Override
                        public void onTextChanged(boolean maskFilled, String extractedValue, String formattedValue) {
                            editBox.SendTextToUnity(formattedValue);
                        }
                    });

            String placeholder = useCustomPlaceholder ? customPlaceholder : maskedTextChangedListener.placeholder();
            edit.setHint(placeholder);

            return maskedTextChangedListener;
        }

        return defaultTextWatcher();
    }

    private TextWatcher defaultTextWatcher() {
        return new TextWatcher() {
            public void afterTextChanged(Editable s) {
                if (characterLimit > 0 && s.length() >= characterLimit + 1) {
                    s.delete(s.length() - 1,
                            s.length());
                    edit.setText(s);
                    edit.setSelection(s.length());
                }
                SendTextToUnity(s.toString());
            }

            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) { }
            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) { }
        };
    }

    private void SendTextToUnity(String text)
    {
        JSONObject jsonToUnity = new JSONObject();
        try
        {
            jsonToUnity.put("msg", MSG_TEXT_CHANGE);
            jsonToUnity.put("text", text);
        }
        catch(JSONException e) {}
        SendJsonToUnity(jsonToUnity);
    }

    private void Remove()
    {
        if (edit != null) {
            layout.removeView(edit);
            mapEditBox.remove(this.tag);
        }
        edit = null;
    }

    private void SetText(String newText)
    {
        if (edit != null) {
            int cursorPos = edit.getSelectionStart();
            int previousLength = edit.getText().length();

            edit.setText(newText);

            // Update text selection (cursor position) to be the same after editing text
            // If the user had multiple characters selected, we are losing them and get the cursor in only one position
            if(previousLength == cursorPos){
                //The cursor was at the end of the text, let us put it again at the end of the text in case more characters were added
                cursorPos = newText.length();
            }

            if(cursorPos > newText.length()){
                //Text was deleted, so cursor position is after the end of the text, let's put it at the last position
                cursorPos = newText.length() + 1;
            }
            edit.setSelection(cursorPos);
        }
    }
    private String GetText()
    {
        return edit.getText().toString();
    }

    private boolean isFocused()
    {
        return edit.isFocused();
    }

    private void SetFocus(boolean isFocus)
    {
        if (isFocus)
        {
            edit.requestFocus();
        }
        else
        {
            edit.clearFocus();

        }
        this.showKeyboard(isFocus);
    }

    private void SetTextSize(JSONObject jsonRect)
    {
        try
        {
            double fontSize = jsonRect.getDouble("fontSize");
            edit.setTextSize(TypedValue.COMPLEX_UNIT_PX, (float) fontSize);
        } catch (JSONException e)
        {
        }
    }

    private void SetRect(JSONObject jsonRect)
    {
        try
        {
            double x = jsonRect.getDouble("x") * (double) layout.getWidth();
            double y = jsonRect.getDouble("y") * (double) layout.getHeight();
            double width = jsonRect.getDouble("width") * (double) layout.getWidth();
            double height = jsonRect.getDouble("height") * (double) layout.getHeight();

            Rect rect = new Rect((int) x, (int) y, (int) (x + width), (int) (y + height));
            RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams(rect.width(), rect.height());
            lp.setMargins(rect.left, rect.top, 0, 0);
            edit.setLayoutParams(lp);

        } catch (JSONException e)
        {
        }
    }

    private void SetVisible(boolean bVisible)
    {
        edit.setEnabled(bVisible);
        edit.setVisibility(bVisible ? View.VISIBLE : View.INVISIBLE);
    }

    private void OnForceAndroidKeyDown(String strKey) {
        if (!this.isFocused()) return;

        // Need to force fire key event of backspace and enter because Unity eats them and never return back to plugin.
        // Same happens on number keys on top of the keyboard with Google Keyboard on password fields.
        int keyCode = -1;
        if (strKey.equalsIgnoreCase("backspace")) {
            keyCode = KeyEvent.KEYCODE_DEL;
        } else if (strKey.equalsIgnoreCase("enter")) {
            keyCode = KeyEvent.KEYCODE_ENTER;
        } else if (strKey.equals("0")) {
            keyCode = KeyEvent.KEYCODE_0;
        } else if (strKey.equals("1")) {
            keyCode = KeyEvent.KEYCODE_1;
        } else if (strKey.equals("2")) {
            keyCode = KeyEvent.KEYCODE_2;
        } else if (strKey.equals("3")) {
            keyCode = KeyEvent.KEYCODE_3;
        } else if (strKey.equals("4")) {
            keyCode = KeyEvent.KEYCODE_4;
        } else if (strKey.equals("5")) {
            keyCode = KeyEvent.KEYCODE_5;
        } else if (strKey.equals("6")) {
            keyCode = KeyEvent.KEYCODE_6;
        } else if (strKey.equals("7")) {
            keyCode = KeyEvent.KEYCODE_7;
        } else if (strKey.equals("8")) {
            keyCode = KeyEvent.KEYCODE_8;
        } else if (strKey.equals("9")) {
            keyCode = KeyEvent.KEYCODE_9;
        }
        if (keyCode > 0) {
            KeyEvent ke = new KeyEvent(KeyEvent.ACTION_DOWN, keyCode);
            Log.i(NativeEditPlugin.LOG_TAG, String.format("Force fire KEY EVENT %d", keyCode));
            edit.onKeyDown(keyCode, ke);
        }
    }

    private static AffinityCalculationStrategy GetAffinityCalculationStrategy(int value)
    {
        switch (value) {
            case 0:
                return AffinityCalculationStrategy.PREFIX;
            case 1:
                return AffinityCalculationStrategy.WHOLE_STRING;
            case 2:
                return AffinityCalculationStrategy.CAPACITY;
            case 3:
                return AffinityCalculationStrategy.EXTRACTED_VALUE_CAPACITY;
            default:
                return AffinityCalculationStrategy.PREFIX;
        }
    }
}
